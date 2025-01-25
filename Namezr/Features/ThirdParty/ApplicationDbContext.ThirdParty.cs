using Microsoft.EntityFrameworkCore;
using Namezr.Features.ThirdParty;

// ReSharper disable once CheckNamespace
namespace Namezr.Infrastructure.Data;

public partial class ApplicationDbContext
{
    private static readonly string ThirdPartyTokenValueProtectorPurpose =
        typeof(ThirdPartyToken).FullName! + "." + nameof(ThirdPartyToken.Value);

    public DbSet<ThirdPartyToken> ThirdPartyTokens { get; set; } = null!;

    private void OnModelCreatingThirdParty(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ThirdPartyToken>()
            .Property(t => t.Value)
            .HasConversion(
                new EncryptedJsonDocumentConverter(
                    _dataProtectionProvider.CreateProtector(ThirdPartyTokenValueProtectorPurpose)
                )
            );
    }
}