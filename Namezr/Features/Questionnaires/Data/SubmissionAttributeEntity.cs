using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Namezr.Client.Shared;

namespace Namezr.Features.Questionnaires.Data;

[EntityTypeConfiguration(typeof(SubmissionAttributeEntityConfiguration))]
public class SubmissionAttributeEntity
{
    public Guid SubmissionId { get; set; }
    public QuestionnaireSubmissionEntity Submission { get; set; } = null!;

    [MaxLength(SubmissionAttributeModel.KeyMaxLength)]
    public required string Key { get; set; }

    [MaxLength(SubmissionAttributeModel.ValueMaxLength)]
    public required string Value { get; set; }
}

internal class SubmissionAttributeEntityConfiguration : IEntityTypeConfiguration<SubmissionAttributeEntity>
{
    public void Configure(EntityTypeBuilder<SubmissionAttributeEntity> builder)
    {
        builder.HasKey(x => new { x.SubmissionId, x.Key });
    }
}