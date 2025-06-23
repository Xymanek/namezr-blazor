using Microsoft.EntityFrameworkCore;

namespace Namezr.Infrastructure.Data;

public static class DbContextExtensions
{
    public static void OnSavedChangesOnce(this DbContext dbContext, EventHandler<SavedChangesEventArgs> callback)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(callback);

        dbContext.SavedChanges += WrappedCallback;
        return;

        void WrappedCallback(object? sender, SavedChangesEventArgs savedChangesEventArgs)
        {
            try
            {
                callback(sender, savedChangesEventArgs);
            }
            finally
            {
                dbContext.SavedChanges -= WrappedCallback;
            }
        }
    }
}