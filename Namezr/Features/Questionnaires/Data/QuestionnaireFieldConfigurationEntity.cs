using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Namezr.Client.Studio.Questionnaires.Edit;

namespace Namezr.Features.Questionnaires.Data;

[EntityTypeConfiguration(typeof(QuestionnaireFieldConfigurationEntityConfiguration))]
public class QuestionnaireFieldConfigurationEntity
{
    public Guid FieldId { get; set; }
    public QuestionnaireFieldEntity Field { get; set; } = null!;

    public Guid VersionId { get; set; }
    public QuestionnaireVersionEntity Version { get; set; } = null!;

    public int Order { get; set; }

    [MaxLength(QuestionnaireFieldEditModel.TitleMaxLength)]
    public required string Title { get; set; }

    [MaxLength(QuestionnaireFieldEditModel.DescriptionMaxLength)]
    public required string? Description { get; set; }

    // TODO: merge these into one DB field

    public QuestionnaireTextFieldOptionsModel? TextOptions { get; set; }
    public QuestionnaireNumberFieldOptionsModel? NumberOptions { get; set; }
    public QuestionnaireFileUploadFieldOptionsModel? FileUploadOptions { get; set; }

    public FieldAutomationType? Automation { get; set; }
}

internal class QuestionnaireFieldConfigurationEntityConfiguration
    : IEntityTypeConfiguration<QuestionnaireFieldConfigurationEntity>
{
    public void Configure(EntityTypeBuilder<QuestionnaireFieldConfigurationEntity> builder)
    {
        builder.HasKey(x => new { x.FieldId, x.VersionId });

        builder.OwnsOne(x => x.TextOptions, n => n.ToJson());
        builder.OwnsOne(x => x.NumberOptions, n => n.ToJson());
        builder.OwnsOne(x => x.FileUploadOptions, n => n.ToJson());
    }
}