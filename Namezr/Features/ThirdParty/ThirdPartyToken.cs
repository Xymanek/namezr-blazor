using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Namezr.Features.ThirdParty;

[EntityTypeConfiguration(typeof(ThirdPartyTokenConfiguration))]
public class ThirdPartyToken
{
    public const string DefaultTokenType = "Default";
    
    public long Id { get; set; }

    [MaxLength(100)]
    public required string ServiceType { get; set; }

    [MaxLength(100)]
    public required string ServiceAccountId { get; set; }

    [MaxLength(100)]
    public required string TokenType { get; set; }

    public required JsonDocument Value { get; set; }

    public JsonDocument? Context { get; set; }

    // TODO: created/updated at?
}

internal class ThirdPartyTokenConfiguration : IEntityTypeConfiguration<ThirdPartyToken>
{
    public void Configure(EntityTypeBuilder<ThirdPartyToken> builder)
    {
        builder.HasIndex(t => new { t.ServiceType, t.ServiceAccountId });
        builder.HasAlternateKey(t => new { t.ServiceType, t.ServiceAccountId, t.TokenType });
    }
}