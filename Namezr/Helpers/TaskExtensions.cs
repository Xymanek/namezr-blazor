namespace Namezr.Helpers;

internal static class TaskExtensions
{
    public static async Task<T[]> WhenAll<T>(this IEnumerable<Task<T>> tasks)
        => await Task.WhenAll(tasks);

    /// <summary>
    /// Creates a task that resolves with <see cref="OperationCanceledException"/>
    /// when the <paramref name="cancellationToken"/> is triggered.
    ///
    /// Does not resolve in any other way.
    /// </summary>
    public static Task AsTask(this CancellationToken cancellationToken)
    {
        TaskCompletionSource tcs = new();
        cancellationToken.Register(() => tcs.TrySetCanceled());

        return tcs.Task;
    }

    /// <summary>
    /// If the <paramref name="cancellationToken"/> is triggered before the task completes,
    /// immediately resolves the returned task with <see cref="OperationCanceledException"/>.
    /// </summary>
    public static async Task<T> DiscardWhenCancelled<T>(this Task<T> task, CancellationToken cancellationToken)
    {
        await Task.WhenAny(task, cancellationToken.AsTask());
        
        // If the task completed, this a very cheap operation.
        // If the cancellation token was triggered, we will not reach here.
        return await task;
    }
}