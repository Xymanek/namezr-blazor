using Microsoft.EntityFrameworkCore;
using Namezr.Features.ThirdParty.Data;

// ReSharper disable once CheckNamespace
namespace Namezr.Infrastructure.Data;

public partial class ApplicationDbContext
{
    // Used to be
    // `typeof(ThirdPartyToken).FullName! + "." + nameof(ThirdPartyToken.Value)`
    // but the class moved to a different namespace and changing the purpose
    // breaks the existing encrypted values
    private const string ThirdPartyTokenValueProtectorPurpose =
        "Namezr.Features.ThirdParty.ThirdPartyToken.Value";

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