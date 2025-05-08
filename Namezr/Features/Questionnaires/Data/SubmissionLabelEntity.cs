using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Namezr.Client.Shared;
using Namezr.Features.Creators.Data;

namespace Namezr.Features.Questionnaires.Data;

[EntityTypeConfiguration(typeof(SubmissionLabelEntityTypeConfiguration))]
public class SubmissionLabelEntity
{
    public Guid Id { get; set; }

    public CreatorEntity Creator { get; set; } = null!;
    public Guid CreatorId { get; set; }

    [MaxLength(SubmissionLabelModel.TextMaxLength)]
    public required string Text { get; set; }

    /// <summary>
    /// Shown on hover
    /// </summary>
    [MaxLength(SubmissionLabelModel.DescriptionMaxLength)]
    public string? Description { get; set; }

    /// <summary>
    /// Must be of hex format, prepended by hash. See
    /// <see cref="M:Namezr.Client.Shared.SubmissionLabelModel.SubmissionLabelModelValidator.GetColourRegex"/>
    /// </summary>
    [MinLength(7)]
    [MaxLength(7)]
    public string? Colour { get; set; }

    // public BootstrapIcon? Icon { get; set; }

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