using System.ComponentModel.DataAnnotations;
using Namezr.Features.Creators.Data;
using Namezr.Features.Eligibility.Data;

namespace Namezr.Features.Polls.Data;

public class PollEntity
{
    public Guid Id { get; set; }

    public CreatorEntity Creator { get; set; } = null!;
    public Guid CreatorId { get; set; }

    [MaxLength(MaxTitleLength)]
    public required string Title { get; set; }

    [MaxLength(MaxDescriptionLength)]
    public required string Description { get; set; }

    /// <summary>
    /// <para>
    /// If <see langword="true"/>, the creator cannot see the choices.
    /// </para>
    /// <para>
    /// Must not be changed after the initial creation.
    /// </para>
    /// </summary>
    // TODO: convert to an enum. E.g. creator can see who voted but not what they voted, public can see who voted, etc.
    public required bool IsAnonymous { get; init; }

    public required bool IsOpen { get; set; }

    // TODO: convert to a dedicated EligibilityConfiguration
    public required bool AllowUsersToAddOptions { get; set; }

    public EligibilityConfigurationEntity EligibilityConfiguration { get; set; } = null!;
    public long EligibilityConfigurationId { get; set; }

    /// <summary>
    /// Must be changed any time the options are changed in any way.
    /// </summary>
    [ConcurrencyCheck]
    public Guid OptionsSetVersionMarker { get; set; } = Guid.NewGuid();

    public ICollection<PollOptionEntity>? Options { get; set; }

    public const int MaxTitleLength = 50;
    public const int MaxDescriptionLength = 500;
}