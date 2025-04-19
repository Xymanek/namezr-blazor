using System.Diagnostics.CodeAnalysis;
using Namezr.Client.Studio.Polls.Edit;
using Namezr.Features.Eligibility.Data;
using Namezr.Features.Polls.Data;
using Riok.Mapperly.Abstractions;

namespace Namezr.Features.Polls.Mappers;

[Mapper]
public static partial class PollFormAndEntityMapper
{
    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    [MapPropertyFromSource(nameof(PollEntity.EligibilityConfiguration), Use = nameof(CreateEligibilityConfiguration))]
    public static partial PollEntity MapToEntity(this PollEditModel source);

    private static EligibilityConfigurationEntity CreateEligibilityConfiguration(PollEditModel source)
    {
        return new EligibilityConfigurationEntity
        {
            OwnershipType = EligibilityConfigurationOwnershipType.Poll,
            Options = EligibilityEntityMapper.Map(source.EligibilityOptions),
        };
    }

    [SuppressMessage(
        "Performance", "CA1859:Use concrete types when possible for improved performance",
        Justification = "Breaks Mapperly's inference"
    )]
    private static ICollection<PollOptionEntity> MapToEntity(List<PollOptionEditModel> source)
    {
        HashSet<PollOptionEntity> entities = [];

        for (var i = 0; i < source.Count; i++)
        {
            entities.Add(MapToEntity(source[i], i));
        }

        return entities;
    }

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    private static partial PollOptionEntity MapToEntity(
        PollOptionEditModel source,
        int order,
        bool wasUserAdded = false
    );
}