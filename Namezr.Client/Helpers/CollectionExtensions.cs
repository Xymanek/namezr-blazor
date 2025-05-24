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
}