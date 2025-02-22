using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Namezr.Features.Identity.Data;
using Namezr.Infrastructure.Data;

namespace Namezr.Components.Account;

internal sealed class IdentityUserAccessor(
    UserManager<ApplicationUser> userManager,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IdentityRedirectManager redirectManager
)
{
    public async Task<ApplicationUser> GetRequiredUserAsync(HttpContext context)
    {
        var user = await GetUserAsync(context);

        if (user is null)
        {
            redirectManager.RedirectToWithStatus("Account/InvalidUser",
                $"Error: Unable to load user with ID '{userManager.GetUserId(context.User)}'.", context);
        }

        return user;
    }

    public async Task<ApplicationUser?> GetUserAsync(HttpContext context)
    {
        string? userIdString = userManager.GetUserId(context.User);
        if (userIdString == null) return null;

        Guid userId = Guid.Parse(userIdString);

        // UserManager (-> UserStore) injects the scoped DbContext, but this is called from
        // the OnInitializedAsync of various components, which are executed in parallel 
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync();

        return await dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId);
    }
}