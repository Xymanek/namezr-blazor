using Namezr.Client.Studio.Eligibility.Edit;
using Namezr.Client.Studio.Polls.Edit;
using Namezr.Features.Eligibility.Data;
using Namezr.Features.Polls.Data;
using Riok.Mapperly.Abstractions;

namespace Namezr.Features.Polls.Mappers;

[Mapper]
internal static partial class PollEntityToFormMapper
{
    [MapperIgnoreSource(nameof(PollEntity.Id))]
    [MapPropertyFromSource(
        nameof(PollEditModel.EligibilityOptions),
        Use = nameof(MapEligibilityToEditModel)
    )]
    public static partial PollEditModel MapToEditModel(this PollEntity source);

    private static List<EligibilityOptionEditModel> MapEligibilityToEditModel(PollEntity poll)
    {
        ICollection<EligibilityOptionEntity> options =
            poll.EligibilityConfiguration.Options
            ?? throw new InvalidOperationException();

        return EligibilityEntityMapper.MapToEditModel(options);
    }

    private static List<PollOptionEditModel> MapToEditModel(ICollection<PollOptionEntity> source)
    {
        return source
            .OrderBy(x => x.Order)
            .Select(MapToEditModel)
            .ToList();
    }

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    private static partial PollOptionEditModel MapToEditModel(this PollOptionEntity source);
}