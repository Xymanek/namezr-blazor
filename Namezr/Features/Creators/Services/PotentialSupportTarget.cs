namespace Namezr.Features.Creators.Services;

internal abstract record PotentialSupportTarget
{
    public abstract SupportServiceType ServiceType { get; }
    public abstract string ServiceId { get; }

    public abstract string DisplayName { get; }

    public required long? ThirdPartyTokenId { get; init; }
}

internal record PotentialTwitchSupportTarget : PotentialSupportTarget
{
    public override SupportServiceType ServiceType => SupportServiceType.Twitch;
    public override string ServiceId => UserTwitchId;

    public override string DisplayName => TwitchDisplayName;

    public required string UserTwitchId { get; init; }
    public required string TwitchDisplayName { get; init; }
    public required string TwitchProfileUrl { get; init; }
    public required string BroadcasterType { get; init; }
}

internal record PotentialPatreonSupportTarget : PotentialSupportTarget
{
    public override SupportServiceType ServiceType => SupportServiceType.Patreon;
    public override string ServiceId => CampaignId;

    public override string DisplayName => Title;

    public required string CampaignId { get; init; }
    public required string Title { get; init; }
    public required string Url { get; init; }
    public required IReadOnlyList<string> Tiers { get; init; }
}