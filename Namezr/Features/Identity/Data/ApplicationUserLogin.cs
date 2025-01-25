using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Namezr.Features.ThirdParty;

namespace Namezr.Features.Identity.Data;

[EntityTypeConfiguration(typeof(ApplicationUserLoginConfiguration))]
public class ApplicationUserLogin : IdentityUserLogin<Guid>
{
    public ThirdPartyToken? ThirdPartyToken { get; set; }
    public long? ThirdPartyTokenId { get; set; }
}

public class ApplicationUserLoginConfiguration : IEntityTypeConfiguration<ApplicationUserLogin>
{
    public void Configure(EntityTypeBuilder<ApplicationUserLogin> builder)
    {
        // Cannot reuse same key entity for multiple associations - would make no sense
        builder.HasIndex(l => l.ThirdPartyTokenId)
            .IsUnique();
    }
}