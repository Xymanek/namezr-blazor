using System.ComponentModel.DataAnnotations;

namespace Namezr.Features.Polls.Data;

public class PollOptionEntity
{
    public Guid Id { get; set; }

    public PollEntity Poll { get; set; } = null!;
    public Guid PollId { get; set; }

    [MaxLength(MaxTitleLength)]
    public required string Title { get; set; }

    [MaxLength(MaxDescriptionLength)]
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

    public const int MaxTitleLength = 30;
    public const int MaxDescriptionLength = 500;
}