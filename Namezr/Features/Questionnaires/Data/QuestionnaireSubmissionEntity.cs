using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodaTime;
using Vogen;

namespace Namezr.Features.Questionnaires.Data;

[ValueObject<Guid>]
public readonly partial struct QuestionnaireSubmissionId;

[EntityTypeConfiguration(typeof(QuestionnaireSubmissionEntityConfiguration))]
public class QuestionnaireSubmissionEntity
{
    public QuestionnaireSubmissionId Id { get; set; }
    
    public QuestionnaireVersionId VersionId { get; set; }
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