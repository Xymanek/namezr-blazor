using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Namezr.Features.Questionnaires.Data;

/// <summary>
/// While this is exposed as an EFC entity, it's mainly used by/via
/// <c>questionnaire_submission_get_next_number_by_version_id</c> - see
/// <see cref="M:Namezr.Infrastructure.Data.Migrations.SubmissionNumber.Up(Microsoft.EntityFrameworkCore.Migrations.MigrationBuilder)"/>
/// </summary>
[EntityTypeConfiguration(typeof(SubmissionNumberSequenceEntityConfiguration))]
public class SubmissionNumberSequenceEntity
{
    public QuestionnaireEntity Questionnaire { get; set; } = null!;
    public Guid QuestionnaireId { get; set; }

    public int Counter { get; set; }
}

internal class SubmissionNumberSequenceEntityConfiguration : IEntityTypeConfiguration<SubmissionNumberSequenceEntity>
{
    public void Configure(EntityTypeBuilder<SubmissionNumberSequenceEntity> builder)
    {
        builder.HasKey(e => e.QuestionnaireId);

        builder.HasOne<QuestionnaireEntity>(sequence => sequence.Questionnaire)
            .WithOne()
            .HasForeignKey<SubmissionNumberSequenceEntity>(e => e.QuestionnaireId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(e => e.Counter)
            .HasDefaultValue(1);
    }
}