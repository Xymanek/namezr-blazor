using System.ComponentModel.DataAnnotations;
using Namezr.Features.Questionnaires.Data;

namespace Namezr.Features.Questionnaires.Models;

public class CannedCommentResponseModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public CannedCommentType CommentType { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class CreateCannedCommentRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [Required]
    public CannedCommentType CommentType { get; set; }

    [Required]
    public string Category { get; set; } = string.Empty;
}

public class UpdateCannedCommentRequest
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [Required]
    public CannedCommentType CommentType { get; set; }

    [Required]
    public string Category { get; set; } = string.Empty;
}