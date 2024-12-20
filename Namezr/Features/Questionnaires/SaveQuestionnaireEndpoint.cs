using FastEndpoints;
using Namezr.Client;
using Namezr.Client.Studio.Questionnaires.Edit;

namespace Namezr.Features.Questionnaires;

public class SaveQuestionnaireEndpoint : Endpoint<QuestionnaireEditModel>
{
    public override void Configure()
    {
        Post(ApiEndpointPaths.QuestionnairesSave);
        AllowAnonymous();
    }

    public override Task<object?> ExecuteAsync(QuestionnaireEditModel req, CancellationToken ct)
    {
        Console.WriteLine("Got save request");

        return Task.FromResult<object?>(null);
    }
}