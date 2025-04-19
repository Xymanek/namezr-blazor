using Riok.Mapperly.Abstractions;

namespace Namezr.Client.Studio.Polls.Edit;

[Mapper(UseDeepCloning = true)]
public static partial class PollModelCloner
{
    public static partial PollEditModel Clone(this PollEditModel source);
}