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
}

[UsedImplicitly]
[EfCoreConverter<QuestionnaireId>]
[EfCoreConverter<QuestionnaireFieldId>]
internal partial class QuestionnaireEfConverters;
