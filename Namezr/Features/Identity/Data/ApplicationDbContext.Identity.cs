using Microsoft.EntityFrameworkCore;
using Namezr.Features.Identity.Data;

// ReSharper disable once CheckNamespace
namespace Namezr.Infrastructure.Data;

public partial class ApplicationDbContext
{
    private static void OnModelCreatingIdentity(ModelBuilder builder)
    {
        // Add the UserLogin -> User navigation property.
        // Since IdentityUserContext's OnModelCreating explicitly sets the navigation
        // as navigation-less, we need to manually configure it here - conventions are disabled.
        // Cannot use [EntityTypeConfiguration] since those happen before IdentityUserContext's OnModelCreating.
        builder.Entity<ApplicationUser>(b =>
        {
            b.HasMany<ApplicationUserLogin>()
                .WithOne(login => login.User)
                .HasForeignKey(ul => ul.UserId)
                .IsRequired();
        });
    }
}