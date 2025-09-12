namespace Namezr.Client.Contracts.Auth;

public interface ISubmissionManagementRequest : IAuthorizableRequest
{
    Guid SubmissionId { get; }
}