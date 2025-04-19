namespace Namezr.Client;

public static class ApiEndpointPaths
{
    public const string QuestionnairesNew = "/api/questionnaires/new";
    public const string QuestionnairesUpdate = "/api/questionnaires/update";
    
    public const string QuestionnaireSubmissionSave = "/api/questionnaires-submissions/save";
    public const string QuestionnaireSubmissionDownloadFile = "/api/questionnaires-submissions/download-file";

    public const string SelectionNewBatch = "/api/selection-series/new-batch";

    public const string FilesUpload = "/api/files/upload";
    public const string FilesDownloadNew = "/api/files/download-new";
    
    public const string CreatorsLogoDownload = "/api/creators/logo-download";
    public const string SupportTargetsLogoDownload = "/api/support-targets/logo-download";
    
    public const string PollsNew = "/api/polls/new";
    public const string PollsUpdate = "/api/polls/update";
}