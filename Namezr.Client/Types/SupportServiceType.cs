namespace Namezr.Client.Types;

// TODO: move to some other namespace?
/// <remarks>
/// Persisted to database - underlying values must never change
/// </remarks>
public enum SupportServiceType
{
    Twitch = 0,
    Patreon = 1,
    // KoFi = 2,
    // BuyMeACoffee = 3,
    Discord = 4,
}