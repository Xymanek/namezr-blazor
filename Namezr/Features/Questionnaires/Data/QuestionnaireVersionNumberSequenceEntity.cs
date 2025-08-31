using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Namezr.Features.Questionnaires.Data;

/// <summary>
/// While this is exposed as an EFC entity, it's mainly used by/via
/// <c>questionnaire_version_get_next_number_by_questionnaire_id</c> - see
/// <see cref="M:Namezr.Migrations.QuestionnaireVersionNumber.Up(Microsoft.EntityFrameworkCore.Migrations.MigrationBuilder)"/>
/// </summary>
[EntityTypeConfiguration(typeof(QuestionnaireVersionNumberSequenceEntityConfiguration))]
public class QuestionnaireVersionNumberSequenceEntity
{
    public QuestionnaireEntity Questionnaire { get; set; } = null!;
    public Guid QuestionnaireId { get; set; }

    public int Counter { get; set; }
}

internal class QuestionnaireVersionNumberSequenceEntityConfiguration : IEntityTypeConfiguration<QuestionnaireVersionNumberSequenceEntity>
{
    public void Configure(EntityTypeBuilder<QuestionnaireVersionNumberSequenceEntity> builder)
    {
        builder.HasKey(e => e.QuestionnaireId);

        builder.HasOne<QuestionnaireEntity>(sequence => sequence.Questionnaire)
            .WithOne()
            .HasForeignKey<QuestionnaireVersionNumberSequenceEntity>(e => e.QuestionnaireId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(e => e.Counter)
            .HasDefaultValue(1);
    }
}