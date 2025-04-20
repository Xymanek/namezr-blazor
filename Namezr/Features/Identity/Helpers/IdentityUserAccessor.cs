using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Namezr.Features.Identity.Data;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Identity.Helpers;

internal sealed class IdentityUserAccessor(
    UserManager<ApplicationUser> userManager,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IdentityRedirectManager redirectManager
)
{
    // TODO: a large amount of GetRequiredUserAsync/GetUserAsync calls should be replaced with GetId calls
    
    public async Task<ApplicationUser> GetRequiredUserAsync(HttpContext context)
    {
        var user = await GetUserAsync(context);

        if (user is null)
        {
            RequiredFailedRedirect(context);
        }

        return user;
    }

    public async Task<ApplicationUser?> GetUserAsync(HttpContext context)
    {
        if (!TryGetUserId(context, out Guid userId)) return null;

        // UserManager (-> UserStore) injects the scoped DbContext, but this is called from
        // the OnInitializedAsync of various components, which are executed in parallel 
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync();

        return await dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId);
    }

    /// <returns>
    /// False if the user is not authenticated.
    /// </returns>
    public bool TryGetUserId(HttpContext context, out Guid userId)
    {
        string? userIdString = userManager.GetUserId(context.User);
        if (userIdString == null)
        {
            // ReSharper disable once PreferConcreteValueOverDefault
            userId = default;
            return false;
        }

        userId = Guid.Parse(userIdString);
        return false;
    }

    public Guid GetRequiredUserId(HttpContext context)
    {
        if (!TryGetUserId(context, out Guid userId))
        {
            RequiredFailedRedirect(context);
        }

        return userId;
    }

    [DoesNotReturn]
    private void RequiredFailedRedirect(HttpContext context)
    {
        redirectManager.RedirectToWithStatus(
            "Account/InvalidUser",
            // TODO: what even is this message?
            $"Error: Unable to load user with ID '{userManager.GetUserId(context.User)}'.",
            context
        );
    }
}