using Microsoft.AspNetCore.Identity;
using Namezr.Features.Identity.Data;

namespace Namezr.Components.Account;

internal sealed class IdentityUserAccessor(
    UserManager<ApplicationUser> userManager,
    IdentityRedirectManager redirectManager)
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

    public Task<ApplicationUser?> GetUserAsync(HttpContext context)
    {
        return userManager.GetUserAsync(context.User);
    }
}