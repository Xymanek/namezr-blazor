using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Namezr.Features.Questionnaires.Data;

[EntityTypeConfiguration(typeof(QuestionnaireFieldValueEntityConfiguration))]
public class QuestionnaireFieldValueEntity
{
    public Guid SubmissionId { get; set; }
    public QuestionnaireSubmissionEntity Submission { get; set; } = null!;

    public Guid FieldId { get; set; }
    public QuestionnaireFieldEntity Field { get; set; } = null!;

    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public required string ValueSerialized { get; set; }
}

internal class QuestionnaireFieldValueEntityConfiguration : IEntityTypeConfiguration<QuestionnaireFieldValueEntity>
{
    public void Configure(EntityTypeBuilder<QuestionnaireFieldValueEntity> builder)
    {
        builder.HasKey(x => new { x.SubmissionId, x.FieldId });
    }
}