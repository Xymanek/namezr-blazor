namespace Namezr.Client.Contracts.Auth;

public interface ICreatorManagementRequest : IAuthorizableRequest
{
    Guid CreatorId { get; }
}
