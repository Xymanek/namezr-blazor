using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Namezr.Features.Identity.Data;
using Namezr.Features.SelectionSeries.Data;
using NodaTime;

namespace Namezr.Features.Questionnaires.Data;

[EntityTypeConfiguration(typeof(QuestionnaireSubmissionEntityConfiguration))]
public class QuestionnaireSubmissionEntity : SelectionCandidateEntity
{
    public QuestionnaireVersionEntity Version { get; set; } = null!;
    public Guid VersionId { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Guid UserId { get; set; }

    public Instant SubmittedAt { get; set; }

    public Instant? ApprovedAt { get; set; }

    /// <summary>
    /// <see cref="ApplicationUser.Id"/> of the user who approved the submission.
    /// Null if the submission is not approved or was approved by the system
    /// (e.g. <see cref="F:Namezr.Client.Studio.Questionnaires.Edit.QuestionnaireApprovalMode.GrantAutomatically"/>).
    /// </summary>
    public Guid? ApproverId { get; set; }

    public ICollection<QuestionnaireFieldValueEntity>? FieldValues { get; set; }
}

internal class QuestionnaireSubmissionEntityConfiguration : IEntityTypeConfiguration<QuestionnaireSubmissionEntity>
{
    public void Configure(EntityTypeBuilder<QuestionnaireSubmissionEntity> builder)
    {
        builder.Property(e => e.SubmittedAt)
            .HasDefaultValueSql("now()");
    }
}