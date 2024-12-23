using FluentValidation;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Client.Studio.Questionnaires.Edit;
using Namezr.Features.Questionnaires.Data;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Questionnaires;

[Handler]
[MapPost(ApiEndpointPaths.QuestionnairesUpdate)]
internal static partial class UpdateQuestionnaireRequest
{
    internal class Request
    {
        public required Guid Id { get; set; }

        // TODO: convert description to null if empty
        public required QuestionnaireEditModel Model { get; set; }
    }

    [RegisterSingleton(typeof(IValidator<Request>))]
    internal class Validator : AbstractValidator<Request>
    {
        public Validator(IValidator<QuestionnaireEditModel> modelValidator)
        {
            RuleFor(x => x.Model)
                .SetValidator(modelValidator);
        }
    }

    private static async ValueTask HandleAsync(
        Request request,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        CancellationToken ct
    )
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        QuestionnaireEntity? questionnaireEntity = await dbContext.Questionnaires
            .Include(x => x.Versions)
            .Include(x => x.Fields)
            .AsSplitQuery()
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (questionnaireEntity is null)
        {
            // TODO: return 400
            throw new Exception("Questionnaire not found");
        }

        new QuestionnaireFormToEntityMapper(questionnaireEntity.Fields!)
            .UpdateEntityWithNewVersion(request.Model, questionnaireEntity);

        await dbContext.SaveChangesAsync(ct);
    }
}