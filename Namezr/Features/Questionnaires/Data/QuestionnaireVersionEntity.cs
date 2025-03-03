using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodaTime;

namespace Namezr.Features.Questionnaires.Data;

[EntityTypeConfiguration(typeof(QuestionnaireVersionEntityConfiguration))]
public class QuestionnaireVersionEntity
{
    public Guid Id { get; set; }

    public Guid QuestionnaireId { get; set; }
    public QuestionnaireEntity Questionnaire { get; set; } = null!;

    public Instant CreatedAt { get; set; }

    public ICollection<QuestionnaireFieldConfigurationEntity>? Fields { get; set; }
    public ICollection<QuestionnaireSubmissionEntity>? Submissions { get; set; }
}

internal class QuestionnaireVersionEntityConfiguration : IEntityTypeConfiguration<QuestionnaireVersionEntity>
{
    public void Configure(EntityTypeBuilder<QuestionnaireVersionEntity> builder)
    {
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()");
    }
}