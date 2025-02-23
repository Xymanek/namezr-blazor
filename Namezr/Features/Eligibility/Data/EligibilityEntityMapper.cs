using Namezr.Client.Studio.Questionnaires.Edit;
using Namezr.Client.Types;
using Riok.Mapperly.Abstractions;
using SuperLinq;

namespace Namezr.Features.Eligibility.Data;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
public static partial class EligibilityEntityMapper
{
    public static ICollection<EligibilityOptionEntity> Map(
        IReadOnlyList<EligibilityOptionEditModel> models,
        ICollection<EligibilityOptionEntity>? entities = null
    )
    {
        entities ??= new HashSet<EligibilityOptionEntity>(models.Count);

        Dictionary<EligibilityId, EligibilityOptionEntity> entitiesById = entities
            .ToDictionary(entity => new EligibilityId(entity.IdChain));

        entitiesById.Keys
            .Except(models.Select(x => x.Id!.Value))
            .ForEach(x => entities.Remove(entitiesById[x]));

        for (int index = 0; index < models.Count; index++)
        {
            EligibilityOptionEditModel model = models[index];

            if (entitiesById.TryGetValue(model.Id!.Value, out EligibilityOptionEntity? entity))
            {
                Map(model, entity);
                entity.Order = index;
            }
            else
            {
                entities.Add(Map(model, index));
            }
        }

        return entities;
    }

    [MapperIgnoreTarget(nameof(EligibilityOptionEntity.Id))]
    private static partial void Map(EligibilityOptionEditModel model, EligibilityOptionEntity entity);

    [MapperIgnoreTarget(nameof(EligibilityOptionEntity.Id))]
    private static partial EligibilityOptionEntity Map(EligibilityOptionEditModel model, int order);
}