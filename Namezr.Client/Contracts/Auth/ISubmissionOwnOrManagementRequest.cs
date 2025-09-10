namespace Namezr.Client.Contracts.Auth;

public interface ISubmissionOwnOrManagementRequest : IAuthorizableRequest
{
    Guid SubmissionId { get; }
}
