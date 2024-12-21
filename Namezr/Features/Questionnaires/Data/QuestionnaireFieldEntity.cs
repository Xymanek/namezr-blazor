using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Namezr.Client.Studio.Questionnaires.Edit;
using Riok.Mapperly.Abstractions;

namespace Namezr.Features.Questionnaires.Data;

[EntityTypeConfiguration(typeof(QuestionnaireFieldEntityConfiguration))]
public class QuestionnaireFieldEntity
{
    public QuestionnaireFieldId Id { get; set; }

    [MapperIgnore] public QuestionnaireId QuestionnaireId { get; set; }
    [MapperIgnore] public QuestionnaireEntity Questionnaire { get; set; } = null!;
    
    [MaxLength(QuestionnaireFieldEditModel.TitleMaxLength)]
    public required string Title { get; set; }

    [MaxLength(QuestionnaireFieldEditModel.DescriptionMaxLength)]
    public required string? Description { get; set; }

    public required QuestionnaireFieldType Type { get; set; }

    // TODO: merge these into one DB field
    
    public QuestionnaireTextFieldOptionsModel? TextOptions { get; set; }
    public QuestionnaireNumberFieldOptionsModel? NumberOptions { get; set; }
    public QuestionnaireFileUploadFieldOptionsModel? FileUploadOptions { get; set; }
}

internal class QuestionnaireFieldEntityConfiguration : IEntityTypeConfiguration<QuestionnaireFieldEntity>
{
    public void Configure(EntityTypeBuilder<QuestionnaireFieldEntity> builder)
    {
        builder.OwnsOne(x => x.TextOptions, n => n.ToJson());
        builder.OwnsOne(x => x.NumberOptions, n => n.ToJson());
        builder.OwnsOne(x => x.FileUploadOptions, n => n.ToJson());
    }
}