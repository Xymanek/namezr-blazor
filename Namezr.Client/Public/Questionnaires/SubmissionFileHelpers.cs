namespace Namezr.Client.Public.Questionnaires;

public static class SubmissionFileHelpers
{
    public static bool IsDisplayableImage(this SubmissionFileData file)
    {
        string[] parts = file.Name.Split('.');
        if (parts.Length < 2) return false;

        return parts[^1] switch
        {
            "jpg" or "jpeg" or "png" or "gif" or "webp" => true,
            _ => false,
        };
    }
}