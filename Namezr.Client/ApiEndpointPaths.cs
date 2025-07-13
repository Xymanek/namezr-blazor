namespace Namezr.Client;

public static class ApiEndpointPaths
{
    public const string QuestionnairesNew = "/api/questionnaires/new";
    public const string QuestionnairesUpdate = "/api/questionnaires/update";

    public const string QuestionnaireSubmissionSave = "/api/questionnaires-submissions/save";
    public const string QuestionnaireSubmissionDownloadFile = "/api/questionnaires-submissions/download-file";

    public const string QuestionnaireSubmissionsBulkDownloadFiles =
        "/api/questionnaires-submissions/bulk-download-file";

    public const string SubmissionLabelsConfigSave = "/api/submission-labels/config/save";
    public const string SubmissionLabelsPresenceMutate = "/api/submission-labels/presence-mutate";

    public const string CannedCommentsConfigSave = "/api/canned-comments/config/save";

    public const string SubmissionAttributesSet = "/api/submission-attributes/set";
    public const string SubmissionAttributesKeys = "/api/submission-attributes/keys";
    public const string SubmissionAttributesValues = "/api/submission-attributes/values";

    public const string SelectionNewBatch = "/api/selection-series/new-batch";

    public const string FilesUpload = "/api/files/upload";
    public const string FilesDownloadNew = "/api/files/download-new";

    public const string CreatorsLogoDownload = "/api/creators/logo-download";
    public const string SupportTargetsLogoDownload = "/api/support-targets/logo-download";

    public const string PollsNew = "/api/polls/new";
    public const string PollsUpdate = "/api/polls/update";
}