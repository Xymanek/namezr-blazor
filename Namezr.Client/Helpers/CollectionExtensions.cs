namespace Namezr.Client.Helpers;

public static class CollectionExtensions
{
    public static void SetIsPresent<T>(this ISet<T> set, bool presence, T element)
    {
        if (presence)
        {
            set.Add(element);
        }
        else
        {
            set.Remove(element);
        }
    }

    public static void AddRange<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        IEnumerable<KeyValuePair<TKey, TValue>> items
    )
    {
        foreach (var item in items)
        {
            dictionary.Add(item.Key, item.Value);
        }
    }
}