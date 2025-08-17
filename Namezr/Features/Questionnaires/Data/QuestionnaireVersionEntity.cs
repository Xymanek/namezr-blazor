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

    /// <summary>
    /// The user-facing "ID" of this version
    /// </summary>
    public int Number { get; set; }

    public Instant CreatedAt { get; set; }

    public ICollection<QuestionnaireFieldConfigurationEntity>? Fields { get; set; }
    public ICollection<QuestionnaireSubmissionEntity>? Submissions { get; set; }
}

internal class QuestionnaireVersionEntityConfiguration : IEntityTypeConfiguration<QuestionnaireVersionEntity>
{
    public void Configure(EntityTypeBuilder<QuestionnaireVersionEntity> builder)
    {
        builder.Property(v => v.Number)
            // The value will be generated only on INSERT, but using "OnAdd" will result in npgsql
            // generating a sequence to populate - which we don't want
            .ValueGeneratedOnAddOrUpdate();

        builder.ToTable(table =>
        {
            // The BEFORE INSERT trigger that achieves the above-mentioned generation logic
            table.HasTrigger("set_version_number");

            // Ensure that the value was successfully populated
            table.HasCheckConstraint("CK_Version_Number_AboveZero", "\"Number\" > 0");
        });

        // Numbers need to be per-questionnaire, so we need uniqueness
        builder.HasIndex(e => new { e.QuestionnaireId, e.Number }).IsUnique();

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()");
    }
}