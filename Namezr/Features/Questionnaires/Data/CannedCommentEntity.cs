using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Namezr.Client.Shared;
using Namezr.Features.Creators.Data;

namespace Namezr.Features.Questionnaires.Data;

[EntityTypeConfiguration(typeof(CannedCommentEntityTypeConfiguration))]
public class CannedCommentEntity
{
    public Guid Id { get; set; }

    public CreatorEntity Creator { get; set; } = null!;
    public Guid CreatorId { get; set; }

    [MaxLength(CannedCommentModel.TitleMaxLength)]
    public required string Title { get; set; }

    [MaxLength(CannedCommentModel.ContentMaxLength)]
    public required string Content { get; set; }

    public required StudioSubmissionCommentType CommentType { get; set; }

    public required bool IsActive { get; set; }
}

internal class CannedCommentEntityTypeConfiguration : IEntityTypeConfiguration<CannedCommentEntity>
{
    public void Configure(EntityTypeBuilder<CannedCommentEntity> builder)
    {
        builder.HasIndex(e => new { e.CreatorId, e.Title })
            .IsUnique();
    }
}