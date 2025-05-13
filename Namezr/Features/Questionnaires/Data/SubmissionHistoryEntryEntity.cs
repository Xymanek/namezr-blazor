using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Namezr.Features.Identity.Data;
using NodaTime;

namespace Namezr.Features.Questionnaires.Data;

public abstract class SubmissionHistoryEntryEntity
{
    public Guid Id { get; set; }

    public QuestionnaireSubmissionEntity Submission { get; set; } = null!;
    public Guid SubmissionId { get; set; }

    public required Instant OccuredAt { get; set; }

    public ApplicationUser? InstigatorUser { get; set; }
    public Guid? InstigatorUserId { get; set; }

    /// <summary>
    /// Allows distinguishing when an action was performed through studio or through public UI.
    /// For cases when staff is also the submitter.
    /// </summary>
    public required bool InstigatorIsStaff { get; set; }

    /// <summary>
    /// True if the action was performed by a completely automated process, not on behalf of a user.
    /// </summary>
    public required bool InstigatorIsProgrammatic { get; set; }

    public const int CommentContentMaxLength = 5000;
    protected const string CommentContentColumnName = "CommentContent";

    protected const string LabelIdColumnName = nameof(SubmissionHistoryLabelAppliedEntity.LabelId);
}

public class SubmissionHistoryLabelAppliedEntity : SubmissionHistoryEntryEntity
{
    /// <summary>
    /// Nullable to accommodate for labels being removed.
    /// </summary>
    public SubmissionLabelEntity? Label { get; set; }

    [Column(LabelIdColumnName)]
    public Guid? LabelId { get; set; }
}

public class SubmissionHistoryLabelRemovedEntity : SubmissionHistoryEntryEntity
{
    /// <summary>
    /// Nullable to accommodate for labels being removed.
    /// </summary>
    public SubmissionLabelEntity? Label { get; set; }

    [Column(LabelIdColumnName)]
    public Guid? LabelId { get; set; }
}

public class SubmissionHistoryFileDownloadedEntity : SubmissionHistoryEntryEntity
{
    public Guid FieldId { get; set; }
    public QuestionnaireFieldEntity Field { get; set; } = null!;

    public required Guid FileId { get; set; }

    /// <summary>
    /// True if the file was downloaded as part of a batch download.
    /// </summary>
    public required bool InBatch { get; set; }
}

public class SubmissionHistoryInitialSubmitEntity : SubmissionHistoryEntryEntity;

public class SubmissionHistoryUpdatedValuesEntity : SubmissionHistoryEntryEntity;

public class SubmissionHistoryApprovalGrantedEntity : SubmissionHistoryEntryEntity;

public class SubmissionHistoryApprovalRemovedEntity : SubmissionHistoryEntryEntity;

/// <summary>
/// A staff note/comment that is NOT visible to the submitter.
/// </summary>
public class SubmissionHistoryInternalNoteEntity : SubmissionHistoryEntryEntity
{
    [Column(CommentContentColumnName)]
    [MaxLength(CommentContentMaxLength)]
    public required string Content { get; set; }
}

/// <summary>
/// A staff comment that is VISIBLE to the submitter OR a comment by the submitter.
/// </summary>
public class SubmissionHistoryPublicCommentEntity : SubmissionHistoryEntryEntity
{
    [Column(CommentContentColumnName)]
    [MaxLength(CommentContentMaxLength)]
    public required string Content { get; set; }
}

/// <summary>
/// Staff opened the details page for the submission.
/// </summary>
public class SubmissionHistoryStaffViewedEntity : SubmissionHistoryEntryEntity;

internal class SubmissionHistoryEntryEntityConfiguration :
    IEntityTypeConfiguration<SubmissionHistoryEntryEntity>,
    IEntityTypeConfiguration<SubmissionHistoryLabelAppliedEntity>,
    IEntityTypeConfiguration<SubmissionHistoryLabelRemovedEntity>
{
    private static readonly SubmissionHistoryEntryEntityConfiguration Instance = new();

    private SubmissionHistoryEntryEntityConfiguration()
    {
    }

    public static void Apply(ModelBuilder modelBuilder)
    {
        // Cannot apply via attribute since the attribute cascades to subclasses and
        // that breaks since the config class does not implement config for subclass interfaces.

        modelBuilder.ApplyConfiguration<SubmissionHistoryEntryEntity>(Instance);
        modelBuilder.ApplyConfiguration<SubmissionHistoryLabelAppliedEntity>(Instance);
        modelBuilder.ApplyConfiguration<SubmissionHistoryLabelRemovedEntity>(Instance);
    }

    public void Configure(EntityTypeBuilder<SubmissionHistoryEntryEntity> builder)
    {
        builder.UseTphMappingStrategy();

        builder.HasDiscriminator<int>("Type")
            .HasValue<SubmissionHistoryLabelAppliedEntity>(0)
            .HasValue<SubmissionHistoryLabelRemovedEntity>(1)
            .HasValue<SubmissionHistoryFileDownloadedEntity>(2)
            .HasValue<SubmissionHistoryInitialSubmitEntity>(3)
            .HasValue<SubmissionHistoryUpdatedValuesEntity>(4)
            .HasValue<SubmissionHistoryApprovalGrantedEntity>(5)
            .HasValue<SubmissionHistoryApprovalRemovedEntity>(6)
            .HasValue<SubmissionHistoryInternalNoteEntity>(7)
            .HasValue<SubmissionHistoryPublicCommentEntity>(8)
            .HasValue<SubmissionHistoryStaffViewedEntity>(9)
            .IsComplete();
    }

    public void Configure(EntityTypeBuilder<SubmissionHistoryLabelAppliedEntity> builder)
    {
        builder.HasOne(x => x.Label)
            .WithMany()
            .HasForeignKey(x => x.LabelId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Navigation(x => x.Label)
            .AutoInclude();
    }

    public void Configure(EntityTypeBuilder<SubmissionHistoryLabelRemovedEntity> builder)
    {
        builder.HasOne(x => x.Label)
            .WithMany()
            .HasForeignKey(x => x.LabelId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Navigation(x => x.Label)
            .AutoInclude();
    }
}