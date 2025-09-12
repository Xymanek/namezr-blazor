namespace Namezr.Client.Contracts.Auth;

public interface IQuestionnaireManagementRequest : IAuthorizableRequest
{
    Guid QuestionnaireId { get; }
}