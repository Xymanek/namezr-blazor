using Namezr.Client.Studio.Questionnaires.Edit;
using Riok.Mapperly.Abstractions;

namespace Namezr.Features.Questionnaires.Data;

[Mapper(UseDeepCloning = true, RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class QuestionnaireEntityToFormMapper
{
    [MapperIgnoreSource(nameof(QuestionnaireVersionEntity.Id))]
    [MapperIgnoreSource(nameof(QuestionnaireVersionEntity.QuestionnaireId))]
    [MapperIgnoreSource(nameof(QuestionnaireVersionEntity.CreatedAt))]
    [MapNestedProperties(nameof(QuestionnaireVersionEntity.Questionnaire))]
    public static partial QuestionnaireEditModel MapToEditModel(this QuestionnaireVersionEntity source);

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

    private static QuestionnaireFieldId Map(QuestionnaireFieldId source) => source;
}

/// <summary>
/// Should be used only for mapping a single instance as field IDs are cached.
/// </summary>
[Mapper(UseDeepCloning = true, RequiredMappingStrategy = RequiredMappingStrategy.Source)]
public partial class QuestionnaireFormToEntityMapper
{
    private readonly Dictionary<QuestionnaireFieldId, QuestionnaireFieldEntity> _fields = new();

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

        return entity;
    }

    [MapperIgnoreSource(nameof(QuestionnaireEntity.Fields))]
    private partial QuestionnaireEntity NewEntityFrom(QuestionnaireEditModel source);

    public void UpdateEntityWithNewVersion(QuestionnaireEditModel source, QuestionnaireEntity target)
    {
        UpdateEntityProperties(source, target);
        target.Versions!.Add(MapToVersionEntity(source));
    }

    [MapperIgnoreSource(nameof(QuestionnaireEntity.Fields))]
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