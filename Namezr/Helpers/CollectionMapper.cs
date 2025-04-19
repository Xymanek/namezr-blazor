using SuperLinq;

namespace Namezr.Helpers;

/// <summary>
/// Replicates https://github.com/AutoMapper/AutoMapper.Collection
/// and adds ability to maintain ordering.
/// </summary>
public static class CollectionMapper
{
    public static ICollection<TTargetItem> Map<TSourceItem, TTargetItem, TKey>(
        Configuration<TSourceItem, TTargetItem, TKey> configuration,
        IReadOnlyList<TSourceItem> sourceList,
        ICollection<TTargetItem>? targetCollection = null
    )
        where TSourceItem : notnull
        where TTargetItem : notnull
        where TKey : IEquatable<TKey>
    {
        targetCollection ??= new HashSet<TTargetItem>(sourceList.Count);

        Dictionary<TKey, TTargetItem> targetsByKey = targetCollection
            .ToDictionary(configuration.TargetKeyProvider);

        targetsByKey.Keys
            .Except(sourceList.Select(configuration.SourceKeyProvider))
            .ForEach(key => targetCollection.Remove(targetsByKey[key]));

        for (int index = 0; index < sourceList.Count; index++)
        {
            TSourceItem sourceItem = sourceList[index];

            if (targetsByKey.TryGetValue(configuration.SourceKeyProvider(sourceItem), out TTargetItem? targetItem))
            {
                configuration.MapUpdate(sourceItem, targetItem, index);
            }
            else
            {
                targetCollection.Add(configuration.MapCreate(sourceItem, index));
            }
        }

        return targetCollection;
    }

    public record Configuration<TSourceItem, TTargetItem, TKey>
        where TSourceItem : notnull
        where TTargetItem : notnull
        where TKey : IEquatable<TKey>
    {
        public required Func<TSourceItem, TKey> SourceKeyProvider { get; init; }
        public required Func<TTargetItem, TKey> TargetKeyProvider { get; init; }

        public required Func<TSourceItem, int, TTargetItem> MapCreate { get; init; }
        public required Action<TSourceItem, TTargetItem, int> MapUpdate { get; init; }
    }
}