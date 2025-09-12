using System.Diagnostics;
using System.Globalization;
using System.Text;
using FluentValidation;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Client.Contracts.Auth;
using Namezr.Client.Contracts.Validation;
using Namezr.Client.Public.Questionnaires;
using Namezr.Client.Studio.Questionnaires.Edit;
using Namezr.Client.Types;
using Namezr.Features.Consumers.Services;
using Namezr.Features.Creators.Services;
using Namezr.Features.Eligibility.Services;
using Namezr.Features.Questionnaires.Data;
using Namezr.Features.Questionnaires.Services;
using Namezr.Infrastructure.Data;
using NodaTime;

namespace Namezr.Features.Questionnaires.Endpoints;

[Handler]
[AutoConstructor]
[Authorize]
[MapGet(ApiEndpointPaths.QuestionnaireSubmissionsExportCsv)]
internal sealed partial class ExportSubmissionsCsvEndpoint
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IFieldValueSerializer _fieldValueSerializer;
    private readonly IEligibilityService _eligibilityService;
    private readonly ISupportPlansService _supportPlansService;

    public class Request : IQuestionnaireManagementRequest, IValidatableRequest
    {
        public required Guid QuestionnaireId { get; init; }

        [RegisterSingleton(typeof(IValidator<Request>))]
        public class Validator : AbstractValidator<Request>
        {
            public Validator()
            {
                RuleFor(x => x.QuestionnaireId)
                    .NotEmpty()
                    .WithMessage("Questionnaire ID is required");
            }
        }
    }

    private async ValueTask<IResult> HandleAsync(
        [AsParameters] Request request,
        CancellationToken ct
    )
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        QuestionnaireEntity? questionnaire = await dbContext.Questionnaires
            .AsNoTracking()
            .Include(q => q.Versions!).ThenInclude(v => v.Fields!).ThenInclude(f => f.Field)
            .Include(q => q.EligibilityConfiguration).ThenInclude(q => q.Options)
            .SingleOrDefaultAsync(x => x.Id == request.QuestionnaireId, ct);

        if (questionnaire == null)
        {
            return Results.BadRequest("Invalid questionnaire ID");
        }

        // Get eligibility descriptors for display names
        List<EligibilityPlan> eligibilityDescriptors = _eligibilityService
            .GetEligibilityDescriptorsFromAllSupportPlans(
                await _supportPlansService.GetSupportPlans(questionnaire.CreatorId)
            )
            .ToList();

        // Get the latest questionnaire version configuration
        QuestionnaireVersionEntity latestVersion = questionnaire.Versions!
            .OrderByDescending(version => version.CreatedAt)
            .First();

        // Load all submissions with their field values
        var submissionsData = await dbContext.QuestionnaireSubmissions
            .AsNoTracking()
            .Where(submission => submission.Version.QuestionnaireId == request.QuestionnaireId)
            .Include(submission => submission.Labels)
            .Include(submission => submission.FieldValues!).ThenInclude(value => value.Field)
            .Include(submission => submission.User)
            .Select(submission => new
            {
                submission,
                LastUpdate = (Instant?)submission.History!
                    .Where(entry =>
                        !(entry is SubmissionHistoryStaffViewedEntity) &&
                        !(entry is SubmissionHistoryFileDownloadedEntity)
                    )
                    .Max(entry => entry.OccuredAt)
            })
            .ToArrayAsync(ct);

        // Get all unique attribute keys across all submissions
        string[] allAttributeKeys = await dbContext.SubmissionAttributes
            .Where(attr => submissionsData.Select(s => s.submission.Id).Contains(attr.SubmissionId))
            .Select(attr => attr.Key)
            .Distinct()
            .OrderBy(k => k)
            .ToArrayAsync(ct);

        // Generate CSV content
        byte[] csvBytes = await GenerateCsvBytes();

        string fileName = $"submissions-{questionnaire.Title.Replace(" ", "-")}-{DateTime.UtcNow:yyyy-MM-dd}.csv";

        return TypedResults.File(csvBytes, "text/csv", fileName);

        async Task<byte[]> GenerateCsvBytes()
        {
            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream, Encoding.UTF8);

            // Write CSV header
            List<string> headers = new()
            {
                "Number",
                "User",
                "Initially Submitted At",
                "Last Update At",
                "Approved",
                "Eligibility Plans",
                "Weight",
                "Labels"
            };

            // Add field headers
            foreach (var fieldVersion in latestVersion.Fields!.OrderBy(f => f.Order))
            {
                headers.Add(fieldVersion.Title);
            }

            // Add attribute headers
            headers.AddRange(allAttributeKeys);

            await writer.WriteLineAsync(string.Join(",", headers.Select(EscapeCsvValue)));

            // Write data rows
            foreach (var submissionData in submissionsData.OrderBy(s => s.submission.Number))
            {
                var submission = submissionData.submission;

                // Get eligibility for this submission
                var eligibility = await _eligibilityService.GetCachedEligibilityOrClassify(
                    submission.UserId,
                    questionnaire.EligibilityConfiguration,
                    UserStatusSyncEagerness.NoSyncSkipConsumerIfMissing
                );

                DateTimeOffset initiallySubmittedAt = submission.SubmittedAt.ToDateTimeOffset();
                DateTimeOffset lastUpdateAt = submissionData.LastUpdate?.ToDateTimeOffset() ?? initiallySubmittedAt;

                List<string> values = new()
                {
                    submission.Number.ToString(),
                    submission.User.UserName ?? "",
                    initiallySubmittedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    lastUpdateAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    submission.ApprovedAt != null ? "Yes" : "No",
                    eligibility.EligiblePlanIds.Count > 0
                        ? string.Join(
                            "; ",
                            eligibility.EligiblePlanIds.Select(planId =>
                                GetEligibilityDescriptorDisplayName(eligibilityDescriptors.Single(x => x.Id == planId))
                            )
                        )
                        : "N/A",
                    eligibility.Modifier.ToString(CultureInfo.InvariantCulture),
                    string.Join("; ", submission.Labels!.Select(l => l.Text))
                };

                // Add field values
                var fieldValuesDict = submission.FieldValues!
                    .ToDictionary(
                        v => v.FieldId,
                        v => _fieldValueSerializer.Deserialize(v.Field.Type, v.ValueSerialized)
                    );

                foreach (var fieldVersion in latestVersion.Fields!.OrderBy(f => f.Order))
                {
                    if (fieldValuesDict.TryGetValue(fieldVersion.FieldId, out var value))
                    {
                        values.Add(FormatFieldValue(value, fieldVersion.Field.Type));
                    }
                    else
                    {
                        values.Add("");
                    }
                }

                // Add attribute values
                var submissionAttributes = await dbContext.SubmissionAttributes
                    .Where(attr => attr.SubmissionId == submission.Id)
                    .ToArrayAsync(ct);
                var attributesDict = submissionAttributes.ToDictionary(a => a.Key, a => a.Value);

                foreach (string attributeKey in allAttributeKeys)
                {
                    values.Add(
                        attributesDict.TryGetValue(attributeKey, out string? attributeValue)
                            ? attributeValue
                            : ""
                    );
                }

                await writer.WriteLineAsync(string.Join(",", values.Select(EscapeCsvValue)));
            }

            await writer.FlushAsync();
            return stream.ToArray();
        }
    }

    private static string FormatFieldValue(SubmissionValueModel value, QuestionnaireFieldType fieldType)
    {
        return fieldType switch
        {
            QuestionnaireFieldType.Text => value.StringValue ?? "",
            QuestionnaireFieldType.Number => value.NumberValue?.ToString(CultureInfo.InvariantCulture) ?? "",
            QuestionnaireFieldType.FileUpload => string.Join("; ", value.FileValue?.Select(f => f.Name) ?? []),
            _ => ""
        };
    }

    private static string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        // Escape quotes and wrap in quotes if contains comma, quote, or newline
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private static string GetEligibilityDescriptorDisplayName(EligibilityPlan plan)
    {
        return plan.Type switch
        {
            EligibilityType.SupportPlan => $"{plan.SupportPlan!.ServiceType} - {plan.SupportPlan!.DisplayName}",
            EligibilityType.Virtual => plan.VirtualEligibilityType!.Value.ToString(),
            _ => throw new UnreachableException(),
        };
    }
}
