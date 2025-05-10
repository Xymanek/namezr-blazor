using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Namezr.Features.Identity.Data;

namespace Namezr.Features.Questionnaires.Data;

public abstract class SubmissionHistoryEntryEntity
{
    public Guid Id { get; set; }

    public QuestionnaireSubmissionEntity Submission { get; set; } = null!;
    public Guid SubmissionId { get; set; }

    public DateTimeOffset OccuredAt { get; set; }

    public ApplicationUser? InstigatorUser { get; set; }
    public Guid? InstigatorUserId { get; set; }

    /// <summary>
    /// Allows distinguishing when an action was performed through studio or through public UI.
    /// For cases when staff is also the submitter.
    /// </summary>
    public bool InstigatorIsStaff { get; set; }

    /// <summary>
    /// True if the action was performed by a completely automated process, not on behalf of a user.
    /// </summary>
    public bool InstigatorIsProgrammatic { get; set; }

    protected const int CommentMaxLength = 5000;
    protected const string CommentColumnName = nameof(SubmissionHistoryInternalCommentEntity.Comment);

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
/// A staff comment that is NOT visible to the submitter.
/// </summary>
public class SubmissionHistoryInternalCommentEntity : SubmissionHistoryEntryEntity
{
    [Column(CommentColumnName)]
    [MaxLength(CommentMaxLength)]
    public required string Comment { get; set; }
}

/// <summary>
/// A staff comment that is VISIBLE to the submitter.
/// </summary>
public class SubmissionHistoryPublicCommentEntity : SubmissionHistoryEntryEntity
{
    [Column(CommentColumnName)]
    [MaxLength(CommentMaxLength)]
    public required string Content { get; set; }
}

/// <summary>
/// A comment by the submitter.
/// </summary>
public class SubmissionHistorySubmitterCommentEntity : SubmissionHistoryEntryEntity
{
    [Column(CommentColumnName)]
    [MaxLength(CommentMaxLength)]
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
            .HasValue<SubmissionHistoryInternalCommentEntity>(7)
            .HasValue<SubmissionHistoryPublicCommentEntity>(8)
            .HasValue<SubmissionHistorySubmitterCommentEntity>(9)
            .HasValue<SubmissionHistoryStaffViewedEntity>(10)
            .IsComplete();
    }

    public void Configure(EntityTypeBuilder<SubmissionHistoryLabelAppliedEntity> builder)
    {
        builder.HasOne(x => x.Label)
            .WithMany()
            .HasForeignKey(x => x.LabelId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<SubmissionHistoryLabelRemovedEntity> builder)
    {
        builder.HasOne(x => x.Label)
            .WithMany()
            .HasForeignKey(x => x.LabelId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}