using Microsoft.EntityFrameworkCore;

namespace Namezr.Infrastructure.Data;

public partial class ApplicationDbContext(DbContextOptions options) : DbContext(options)
{
    public static void DefaultConfigure(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
        optionsBuilder.UseNpgsql(postgres => postgres.UseNodaTime());
    }
}