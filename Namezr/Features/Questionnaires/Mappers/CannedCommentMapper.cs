using Namezr.Client.Shared;
using Namezr.Features.Questionnaires.Data;
using Riok.Mapperly.Abstractions;

namespace Namezr.Features.Questionnaires.Mappers;

[Mapper]
public static partial class CannedCommentMapper
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial CannedCommentModel ToModel(this CannedCommentEntity entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial CannedCommentEntity ToEntity(this CannedCommentModel model);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial void ToEntity(this CannedCommentModel model, CannedCommentEntity entity);
}