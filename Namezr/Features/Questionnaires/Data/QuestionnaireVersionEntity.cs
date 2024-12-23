using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Namezr.Client.Studio.Questionnaires.Edit;
using NodaTime;
using Vogen;

namespace Namezr.Features.Questionnaires.Data;

[ValueObject<Guid>]
public readonly partial struct QuestionnaireVersionId;

[EntityTypeConfiguration(typeof(QuestionnaireVersionEntityConfiguration))]
public class QuestionnaireVersionEntity
{
    public QuestionnaireVersionId Id { get; set; }

    public QuestionnaireId QuestionnaireId { get; set; }
    public QuestionnaireEntity Questionnaire { get; set; } = null!;

    public Instant CreatedAt { get; set; }

    public ICollection<QuestionnaireFieldConfigurationEntity>? Fields { get; set; }
}

internal class QuestionnaireVersionEntityConfiguration : IEntityTypeConfiguration<QuestionnaireVersionEntity>
{
    public void Configure(EntityTypeBuilder<QuestionnaireVersionEntity> builder)
    {
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()");
    }
}