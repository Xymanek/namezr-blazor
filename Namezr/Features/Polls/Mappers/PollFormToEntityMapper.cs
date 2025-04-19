using System.Diagnostics.CodeAnalysis;
using Namezr.Client.Studio.Polls.Edit;
using Namezr.Features.Eligibility.Data;
using Namezr.Features.Polls.Data;
using Namezr.Helpers;
using Riok.Mapperly.Abstractions;

namespace Namezr.Features.Polls.Mappers;

[Mapper]
public static partial class PollFormToEntityMapper
{
    private static readonly CollectionMapper.Configuration<PollOptionEditModel, PollOptionEntity, Guid>
        OptionMappingConfiguration = new()
        {
            SourceKeyProvider = model => model.Id,
            TargetKeyProvider = entity => entity.Id,

            MapCreate = MapToEntity,
            MapUpdate = UpdateEntity,
        };

    #region NewEntity

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
        return CollectionMapper.Map(OptionMappingConfiguration, source);
    }

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    [MapValue(nameof(PollOptionEntity.WasUserAdded), false)]
    private static partial PollOptionEntity MapToEntity(
        PollOptionEditModel source,
        int order
    );

    #endregion

    #region ExistingEntity

    public static void UpdateEntity(this PollEditModel source, PollEntity target)
    {
        DoUpdateEntity(source, target);
        CollectionMapper.Map(OptionMappingConfiguration, source.Options, target.Options);
        EligibilityEntityMapper.Map(source.EligibilityOptions, target.EligibilityConfiguration.Options);
    }

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    [MapperIgnoreSource(nameof(PollEditModel.IsAnonymous))] // Immutable
    [MapperIgnoreSource(nameof(PollEditModel.Options))] // Done manually above
    [MapperIgnoreSource(nameof(PollEditModel.EligibilityOptions))] // Done manually above
    private static partial void DoUpdateEntity(PollEditModel source, PollEntity target);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    private static partial void UpdateEntity(
        PollOptionEditModel source,
        PollOptionEntity target,
        int order
    );

    #endregion
}