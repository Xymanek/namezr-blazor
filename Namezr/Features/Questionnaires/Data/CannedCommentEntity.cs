using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Namezr.Features.Identity.Data;
using NodaTime;

namespace Namezr.Features.Questionnaires.Data;

[EntityTypeConfiguration(typeof(CannedCommentEntityConfiguration))]
public class CannedCommentEntity
{
    public Guid Id { get; set; }
    public Guid CreatorId { get; set; }
    public ApplicationUser Creator { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public CannedCommentType CommentType { get; set; }
    public string Category { get; set; } = string.Empty;
    public Instant CreatedAt { get; set; }
    public Instant UpdatedAt { get; set; }
}

public enum CannedCommentType
{
    InternalNote,
    PublicComment,
}

internal class CannedCommentEntityConfiguration : IEntityTypeConfiguration<CannedCommentEntity>
{
    public void Configure(EntityTypeBuilder<CannedCommentEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Content)
            .IsRequired();

        builder.Property(e => e.Category)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.CommentType)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.HasOne(e => e.Creator)
            .WithMany()
            .HasForeignKey(e => e.CreatorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("CannedComments");
    }
}