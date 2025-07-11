namespace Namezr.Client.Studio.Questionnaires;

public class CannedCommentModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string CommentType { get; set; } = string.Empty; // "Internal" or "Public"
    public string Category { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}