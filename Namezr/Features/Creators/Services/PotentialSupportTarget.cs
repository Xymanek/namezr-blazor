namespace Namezr.Features.Creators.Services;

internal record PotentialSupportTarget
{
    // public required Guid UserLoginId { get; init; }
}

internal record PotentialTwitchSupportTarget : PotentialSupportTarget
{
    public required string UserTwitchId { get; init; }
    public required string TwitchDisplayName { get; init; }
    public required string TwitchProfileUrl { get; init; }
    public required string BroadcasterType { get; init; }
}

internal record PotentialPatreonSupportTarget : PotentialSupportTarget
{
    public required string CampaignId { get; init; }
    public required string Title { get; init; }
    public required string Url { get; init; }
    public required IReadOnlyList<string> Tiers { get; init; }
}