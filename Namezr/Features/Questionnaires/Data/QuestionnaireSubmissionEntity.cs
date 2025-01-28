using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Namezr.Features.Identity.Data;
using NodaTime;

namespace Namezr.Features.Questionnaires.Data;

[EntityTypeConfiguration(typeof(QuestionnaireSubmissionEntityConfiguration))]
public class QuestionnaireSubmissionEntity
{
    public Guid Id { get; set; }

    public QuestionnaireVersionEntity Version { get; set; } = null!;
    public Guid VersionId { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Guid UserId { get; set; }

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