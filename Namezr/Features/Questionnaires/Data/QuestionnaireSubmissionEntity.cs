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

    /// <summary>
    /// The user-facing "ID" of this submission
    /// </summary>
    public int Number { get; set; }

    public Instant SubmittedAt { get; set; }

    public Instant? ApprovedAt { get; set; }

    /// <summary>
    /// <see cref="ApplicationUser.Id"/> of the user who approved the submission.
    /// Null if the submission is not approved or was approved by the system
    /// (e.g. <see cref="F:Namezr.Client.Studio.Questionnaires.Edit.QuestionnaireApprovalMode.GrantAutomatically"/>).
    /// </summary>
    public Guid? ApproverId { get; set; }

    public ICollection<QuestionnaireFieldValueEntity>? FieldValues { get; set; }

    public ICollection<SubmissionLabelEntity>? Labels { get; set; }
    public ICollection<SubmissionLabelLinkEntity>? LabelLinks { get; set; }
}

/// <remarks>
/// Check
/// <see cref="M:Namezr.Infrastructure.Data.Migrations.SubmissionNumber.Up(Microsoft.EntityFrameworkCore.Migrations.MigrationBuilder)"/>
/// for the actual SQL of the functions and triggers
/// </remarks>
internal class QuestionnaireSubmissionEntityConfiguration : IEntityTypeConfiguration<QuestionnaireSubmissionEntity>
{
    public void Configure(EntityTypeBuilder<QuestionnaireSubmissionEntity> builder)
    {
        builder.Property(s => s.Number)
            // The value will be generated only on INSERT, but using "OnAdd" will result in npgsql
            // generating a sequence to populate - which we don't want
            .ValueGeneratedOnAddOrUpdate();

        builder.ToTable(table =>
        {
            // The BEFORE INSERT trigger that achieves the above-mentioned generation logic
            table.HasTrigger("set_number");

            // Ensure that the value was successfully populated
            table.HasCheckConstraint("CK_Number_AboveZero", "\"Number\" > 0");
        });

        // Numbers need to be per-submission, so we also need a trigger to validate
        // the 2nd level uniqueness (submission -> version -> questionnaire).
        builder.HasIndex(e => new { e.VersionId, e.Number }).IsUnique();
        builder.ToTable(table => table.HasTrigger("validate_number_per_questionnaire"));

        builder.Property(e => e.SubmittedAt)
            .HasDefaultValueSql("now()");
    }
}