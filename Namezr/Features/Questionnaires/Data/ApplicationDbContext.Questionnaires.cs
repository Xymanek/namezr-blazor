using Microsoft.EntityFrameworkCore;
using Namezr.Features.Questionnaires.Data;

// ReSharper disable once CheckNamespace
namespace Namezr.Infrastructure.Data;

public partial class ApplicationDbContext
{
    public DbSet<QuestionnaireEntity> Questionnaires { get; set; } = null!;

    public DbSet<QuestionnaireFieldEntity> QuestionnaireFields { get; set; } = null!;
    public DbSet<QuestionnaireFieldConfigurationEntity> QuestionnaireFieldConfigurations { get; set; } = null!;
    public DbSet<QuestionnaireVersionEntity> QuestionnaireVersions { get; set; } = null!;

    public DbSet<QuestionnaireSubmissionEntity> QuestionnaireSubmissions { get; set; } = null!;
    public DbSet<QuestionnaireFieldValueEntity> QuestionnaireFieldValues { get; set; } = null!;
    public DbSet<SubmissionHistoryEntryEntity> SubmissionHistoryEntries { get; set; } = null!;

    public DbSet<SubmissionNumberSequenceEntity> SubmissionNumberSequences { get; set; } = null!;

    public DbSet<SubmissionLabelEntity> SubmissionLabels { get; set; } = null!;
    public DbSet<SubmissionAttributeEntity> SubmissionAttributes { get; set; } = null!;

    private static void OnModelCreatingQuestionnaires(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SubmissionLabelEntity>()
            .HasMany(label => label.Submissions)
            .WithMany(submission => submission.Labels)
            .UsingEntity<SubmissionLabelLinkEntity>(
                j => j.HasOne<QuestionnaireSubmissionEntity>(link => link.Submission)
                    .WithMany(submission => submission.LabelLinks)
                    .HasForeignKey(link => link.SubmissionId),
                j => j.HasOne<SubmissionLabelEntity>(link => link.Label)
                    .WithMany(label => label.SubmissionLinks)
                    .HasForeignKey(link => link.LabelId),
                j => j.ToTable("QuestionnaireSubmissions_Labels")
            );

        SubmissionHistoryEntryEntityConfiguration.Apply(modelBuilder);
    }
}