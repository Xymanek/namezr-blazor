using Namezr.Client.Shared;
using Namezr.Features.Questionnaires.Data;
using Riok.Mapperly.Abstractions;

namespace Namezr.Features.Questionnaires.Mappers;

[Mapper]
public static partial class SubmissionLabelMapper
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial SubmissionLabelModel ToModel(this SubmissionLabelEntity entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial SubmissionLabelEntity ToEntity(this SubmissionLabelModel model);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial void ToEntity(this SubmissionLabelModel model, SubmissionLabelEntity entity);
}