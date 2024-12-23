using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Namezr.Client.Studio.Questionnaires.Edit;
using Namezr.Features.Questionnaires.Data;
using Vogen;

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
}

[UsedImplicitly]
[EfCoreConverter<QuestionnaireId>]
[EfCoreConverter<QuestionnaireFieldId>]
[EfCoreConverter<QuestionnaireVersionId>]
[EfCoreConverter<QuestionnaireSubmissionId>]
internal partial class QuestionnaireEfConverters;