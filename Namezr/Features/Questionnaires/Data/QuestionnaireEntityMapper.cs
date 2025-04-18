using Namezr.Client.Studio.Questionnaires;
using Namezr.Client.Studio.Questionnaires.Edit;
using Namezr.Features.Eligibility.Data;
using NodaTime;
using Riok.Mapperly.Abstractions;

namespace Namezr.Features.Questionnaires.Data;

[Mapper(UseDeepCloning = true, RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class QuestionnaireEntityToFormMapper
{
    [MapperIgnoreSource(nameof(QuestionnaireVersionEntity.Id))]
    [MapperIgnoreSource(nameof(QuestionnaireVersionEntity.QuestionnaireId))]
    [MapperIgnoreSource(nameof(QuestionnaireVersionEntity.CreatedAt))]
    [MapNestedProperties(nameof(QuestionnaireVersionEntity.Questionnaire))]
    [MapPropertyFromSource(
        nameof(QuestionnaireEditModel.EligibilityOptions),
        Use = nameof(MapEligibilityToEditModel)
    )]
    public static partial QuestionnaireEditModel MapToEditModel(this QuestionnaireVersionEntity source);

    private static List<EligibilityOptionEditModel> MapEligibilityToEditModel(
        QuestionnaireVersionEntity questionnaireVersion
    )
    {
        ICollection<EligibilityOptionEntity> options =
            questionnaireVersion.Questionnaire.EligibilityConfiguration.Options
            ?? throw new InvalidOperationException();

        return MapToEditModel(options);
    }

    private static partial List<EligibilityOptionEditModel> MapToEditModel(ICollection<EligibilityOptionEntity> o);

    [UserMapping(Default = true)]
    private static List<QuestionnaireFieldEditModel> MapToEditModel(
        ICollection<QuestionnaireFieldConfigurationEntity> source
    )
    {
        List<QuestionnaireFieldEditModel> target = new(source.Count);
        target.AddRange(
            source
                .OrderBy(x => x.Order)
                .Select(MapToEditModel)
        );

        return target;
    }

    [MapperIgnoreSource(nameof(QuestionnaireFieldConfigurationEntity.FieldId))]
    [MapperIgnoreSource(nameof(QuestionnaireFieldConfigurationEntity.Version))]
    [MapperIgnoreSource(nameof(QuestionnaireFieldConfigurationEntity.VersionId))]
    [MapperIgnoreSource(nameof(QuestionnaireFieldConfigurationEntity.Order))]
    [MapNestedProperties(nameof(QuestionnaireFieldConfigurationEntity.Field))]
    private static partial QuestionnaireFieldEditModel MapToEditModel(QuestionnaireFieldConfigurationEntity source);

    public static partial VersionOverviewModel[] MapToOverview(IEnumerable<QuestionnaireVersionEntity> source);

    private static DateTimeOffset Map(Instant source)
    {
        return source.ToDateTimeOffset();
    }
}

/// <summary>
/// Should be used only for mapping a single instance as field IDs are cached.
/// </summary>
[Mapper(UseDeepCloning = true, RequiredMappingStrategy = RequiredMappingStrategy.Source)]
public partial class QuestionnaireFormToEntityMapper
{
    private readonly Dictionary<Guid, QuestionnaireFieldEntity> _fields = new();

    public QuestionnaireFormToEntityMapper()
    {
    }

    public QuestionnaireFormToEntityMapper(IEnumerable<QuestionnaireFieldEntity> existingFields)
    {
        foreach (QuestionnaireFieldEntity entity in existingFields)
        {
            _fields.Add(entity.Id, entity);
        }
    }

    public QuestionnaireEntity MapToEntity(QuestionnaireEditModel source)
    {
        QuestionnaireEntity entity = NewEntityFrom(source);

        entity.Versions = new HashSet<QuestionnaireVersionEntity>
        {
            MapToVersionEntity(source),
        };
        UpdateFields(entity);

        entity.EligibilityConfiguration = new EligibilityConfigurationEntity
        {
            OwnershipType = EligibilityConfigurationOwnershipType.Questionnaire,
            Options = EligibilityEntityMapper.Map(source.EligibilityOptions),
        };

        return entity;
    }

    /// <summary>
    /// Must be after <see cref="P:Namezr.Features.Questionnaires.Data.QuestionnaireEntity.Versions"/>
    /// is mapped as that populates the <see cref="_fields"/>.
    /// </summary>
    private void UpdateFields(QuestionnaireEntity entity)
    {
        if (entity.Fields == null)
        {
            entity.Fields = _fields.Values.ToHashSet();
            return;
        }

        foreach (QuestionnaireFieldEntity fieldEntity in _fields.Values.Except(entity.Fields))
        {
            entity.Fields.Add(fieldEntity);
        }
    }

    [MapperIgnoreSource(nameof(QuestionnaireEditModel.Fields))]
    [MapperIgnoreSource(nameof(QuestionnaireEditModel.EligibilityOptions))]
    private partial QuestionnaireEntity NewEntityFrom(QuestionnaireEditModel source);

    public void UpdateEntityWithNewVersion(QuestionnaireEditModel source, QuestionnaireEntity target)
    {
        UpdateEntityProperties(source, target);
        target.Versions!.Add(MapToVersionEntity(source));
        EligibilityEntityMapper.Map(source.EligibilityOptions, target.EligibilityConfiguration.Options);
        UpdateFields(target);
    }

    [MapperIgnoreSource(nameof(QuestionnaireEditModel.Fields))]
    [MapperIgnoreSource(nameof(QuestionnaireEditModel.EligibilityOptions))]
    private partial void UpdateEntityProperties(QuestionnaireEditModel source, QuestionnaireEntity target);

    private QuestionnaireVersionEntity MapToVersionEntity(QuestionnaireEditModel source) => new()
    {
        Fields = MapToEntity(source.Fields),
    };

    private ICollection<QuestionnaireFieldConfigurationEntity> MapToEntity(
        List<QuestionnaireFieldEditModel> source
    )
    {
        ICollection<QuestionnaireFieldConfigurationEntity> target =
            new HashSet<QuestionnaireFieldConfigurationEntity>(source.Count);

        for (var i = 0; i < source.Count; i++)
        {
            QuestionnaireFieldConfigurationEntity entity = MapToEntity(source[i]);
            entity.Order = i;

            target.Add(entity);
        }

        return target;
    }

    [MapPropertyFromSource(nameof(QuestionnaireFieldConfigurationEntity.Field), Use = nameof(GetFieldEntity))]
    private partial QuestionnaireFieldConfigurationEntity MapToEntity(QuestionnaireFieldEditModel source);

    private QuestionnaireFieldEntity GetFieldEntity(QuestionnaireFieldEditModel source)
    {
        if (_fields.TryGetValue(source.Id, out QuestionnaireFieldEntity? entity))
        {
            if (entity.Type != source.Type)
            {
                throw new Exception("Field type mismatch");
            }

            return entity;
        }

        entity = new QuestionnaireFieldEntity
        {
            Id = source.Id,
            Type = source.Type!.Value,
        };

        _fields.Add(source.Id, entity);
        return entity;
    }
}