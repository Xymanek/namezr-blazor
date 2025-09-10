namespace Namezr.Client.Contracts.Auth;

public interface IPollManagementRequest : IAuthorizableRequest
{
    Guid PollId { get; }
}