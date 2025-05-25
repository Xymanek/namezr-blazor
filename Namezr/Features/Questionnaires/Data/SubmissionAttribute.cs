using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Namezr.Features.Questionnaires.Data;

[EntityTypeConfiguration(typeof(SubmissionAttributeConfiguration))]
public class SubmissionAttributeEntity
{
    public QuestionnaireSubmissionEntity Submission { get; set; } = null!;
    public Guid SubmissionId { get; set; }

    [MaxLength(KeyMaxLength)]
    public required string Key { get; init; }

    [MaxLength(ValueMaxLength)]
    public required string Value { get; set; }

    internal const int KeyMaxLength = 50;
    internal const int ValueMaxLength = 500;
}

internal class SubmissionAttributeConfiguration : IEntityTypeConfiguration<SubmissionAttributeEntity>
{
    public void Configure(EntityTypeBuilder<SubmissionAttributeEntity> builder)
    {
        builder.HasKey(x => new { x.SubmissionId, x.Key });
    }
}