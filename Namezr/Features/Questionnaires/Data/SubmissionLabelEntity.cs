using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Namezr.Features.Creators.Data;

namespace Namezr.Features.Questionnaires.Data;

[EntityTypeConfiguration(typeof(SubmissionLabelEntityTypeConfiguration))]
public class SubmissionLabelEntity
{
    public Guid Id { get; set; }

    public CreatorEntity Creator { get; set; } = null!;
    public Guid CreatorId { get; set; }

    public required string Text { get; set; }

    /// <summary>
    /// Shown on hover
    /// </summary>
    public string? Description { get; set; }

    public string? Colour { get; set; }

    // TODO: store as text, not index (as we do not control the indices)
    public BootstrapIcon? Icon { get; set; }

    public required bool IsSubmitterVisible { get; set; }
}

internal class SubmissionLabelEntityTypeConfiguration : IEntityTypeConfiguration<SubmissionLabelEntity>
{
    public void Configure(EntityTypeBuilder<SubmissionLabelEntity> builder)
    {
        builder.HasIndex(e => new { e.CreatorId, e.Text })
            .IsUnique();
    }
}