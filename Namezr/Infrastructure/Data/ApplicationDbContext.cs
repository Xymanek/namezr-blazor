using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Namezr.Features.Identity.Data;

namespace Namezr.Infrastructure.Data;

public partial class ApplicationDbContext(DbContextOptions options)
    : IdentityUserContext<ApplicationUser, Guid>(options)
{
    public static void DefaultConfigure(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
        optionsBuilder.UseNpgsql(postgres => postgres.UseNodaTime());
    }
}