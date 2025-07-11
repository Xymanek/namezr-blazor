using System;

namespace Namezr.Client.Studio.Questionnaires;

public class CannedCommentSaveRequest
{
    public Guid? Id { get; set; } // null for create, set for update
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string CommentType { get; set; } = string.Empty; // "Internal" or "Public"
    public string Category { get; set; } = string.Empty;
}