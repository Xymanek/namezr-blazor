using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodaTime;

namespace Namezr.Features.Questionnaires.Data;

[EntityTypeConfiguration(typeof(QuestionnaireSubmissionEntityConfiguration))]
public class QuestionnaireSubmissionEntity
{
    public Guid Id { get; set; }
    
    public Guid VersionId { get; set; }
    public QuestionnaireVersionEntity Version { get; set; } = null!;

    public Instant SubmittedAt { get; set; }
    
    // TODO: approval

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