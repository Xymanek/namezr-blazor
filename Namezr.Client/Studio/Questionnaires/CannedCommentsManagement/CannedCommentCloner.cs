using Namezr.Client.Shared;
using Riok.Mapperly.Abstractions;

namespace Namezr.Client.Studio.Questionnaires.CannedCommentsManagement;

[Mapper]
public static partial class CannedCommentCloner
{
    public static partial CannedCommentModel Clone(this CannedCommentModel model);
}