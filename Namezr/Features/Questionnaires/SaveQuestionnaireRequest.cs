using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Namezr.Client;
using Namezr.Client.Studio.Questionnaires.Edit;

namespace Namezr.Features.Questionnaires;

[Handler]
[MapPost(ApiEndpointPaths.QuestionnairesSave)]
public static partial class SaveQuestionnaireRequest
{
    private static async ValueTask HandleAsync(QuestionnaireEditModel model, CancellationToken ct)
    {
        Console.WriteLine("Got save request");
        await Task.CompletedTask;
    }
}