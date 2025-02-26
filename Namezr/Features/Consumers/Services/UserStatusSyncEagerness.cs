namespace Namezr.Features.Consumers.Services;

public enum UserStatusSyncEagerness
{
    /// <summary>
    /// Same as <see cref="NoSync"/> but will also skip creating consumer records if they do not already exist.
    /// </summary>
    NoSyncSkipConsumerIfMissing,

    /// <summary>
    /// Will never sync under any circumstances - returned info is only from the app database.
    /// </summary>
    NoSync,

    /// <summary>
    /// Will sync only if the user has no status in the app database.
    /// Otherwise, same as <see cref="NoSync"/>.
    /// </summary>
    MissingStatusOnly,

    /// <summary>
    /// Will sync if the DB status is outdated or missing.
    /// </summary>
    Default,

    /// <summary>
    /// Will always sync.
    /// </summary>
    Force,
}