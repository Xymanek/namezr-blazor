namespace Namezr.Client.Contracts.Auth;

public interface ISeriesManagementRequest : IAuthorizableRequest
{
    Guid SeriesId { get; }
}