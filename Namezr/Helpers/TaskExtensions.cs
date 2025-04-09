namespace Namezr.Helpers;

internal static class TaskExtensions
{
    public static async Task<T[]> WhenAll<T>(this IEnumerable<Task<T>> tasks) 
        => await Task.WhenAll(tasks);
}