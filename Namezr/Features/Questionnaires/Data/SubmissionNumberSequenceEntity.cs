using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Namezr.Features.Questionnaires.Data;

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