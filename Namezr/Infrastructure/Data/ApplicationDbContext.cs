using EntityFramework.Exceptions.PostgreSQL;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Namezr.Features.Identity.Data;

namespace Namezr.Infrastructure.Data;

public partial class ApplicationDbContext : IdentityUserContext<
    ApplicationUser,
    Guid,
    IdentityUserClaim<Guid>,
    ApplicationUserLogin,
    IdentityUserToken<Guid>
>
{
    private readonly IDataProtectionProvider _dataProtectionProvider;

    public ApplicationDbContext(DbContextOptions options, IDataProtectionProvider dataProtectionProvider) :
        base(options)
    {
        _dataProtectionProvider = dataProtectionProvider;
    }

    public static void DefaultConfigure(DbContextOptionsBuilder optionsBuilder)
    {
        // TODO: does identity work with NoTracking?
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);

        optionsBuilder.UseNpgsql(postgres => postgres.UseNodaTime());

        optionsBuilder.UseExceptionProcessor();

        // optionsBuilder.EnableSensitiveDataLogging();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        OnModelCreatingThirdParty(builder);
        OnModelCreatingIdentity(builder);
    }
}