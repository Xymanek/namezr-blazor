using Microsoft.AspNetCore.Identity;

namespace Namezr.Features.Identity.Data;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public ApplicationUser()
    {
        Id = Guid.NewGuid();
    }
}