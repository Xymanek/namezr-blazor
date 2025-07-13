using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Namezr.Features.Questionnaires.Data;

[EntityTypeConfiguration(typeof(SubmissionAttributeEntityConfiguration))]
public class SubmissionAttributeEntity
{
    public Guid SubmissionId { get; set; }
    public QuestionnaireSubmissionEntity Submission { get; set; } = null!;

    public required string Key { get; set; }
    public required string Value { get; set; }
}

internal class SubmissionAttributeEntityConfiguration : IEntityTypeConfiguration<SubmissionAttributeEntity>
{
    public void Configure(EntityTypeBuilder<SubmissionAttributeEntity> builder)
    {
        builder.HasKey(x => new { x.SubmissionId, x.Key });
        
        builder.Property(x => x.Key)
            .HasMaxLength(50);
    }
}