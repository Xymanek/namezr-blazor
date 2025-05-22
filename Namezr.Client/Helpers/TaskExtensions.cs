namespace Namezr.Client.Helpers;

public static class TaskExtensions
{
    public static Task WhenAll(this IEnumerable<Task> tasks)
    {
        return Task.WhenAll(tasks);
    }
}