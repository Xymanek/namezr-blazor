using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Namezr.Client.Studio.Polls.Edit;

namespace Namezr.Features.Polls.Data;

public class PollOptionEntity
{
    public Guid Id { get; set; }

    public PollEntity Poll { get; set; } = null!;
    public Guid PollId { get; set; }

    [MaxLength(PollOptionEditModel.TitleMaxLength)]
    public required string Title { get; set; }

    [MaxLength(PollOptionEditModel.DescriptionMaxLength)]
    public required string Description { get; set; }

    public required int Order { get; set; }

    /// <summary>
    /// <para>
    /// True if this was originally added outside the studio UI,
    /// due to <see cref="PollEntity.AllowUsersToAddOptions"/>.
    /// </para>
    /// <para>
    /// Stays true even if later edited in the studio UI.
    /// </para>
    /// </summary>
    public required bool WasUserAdded { get; init; }
}

internal class PollOptionEntityConfiguration : IEntityTypeConfiguration<PollOptionEntity>
{
    public void Configure(EntityTypeBuilder<PollOptionEntity> builder)
    {
        builder.HasIndex(option => new { option.PollId, option.Title });
    }
}