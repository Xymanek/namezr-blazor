using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Namezr.Features.Identity.Data;
using Namezr.Features.ThirdParty;

namespace Namezr.Features.Creators.Data;

/// <summary>
/// A place were people supporters can spend money.
/// E.g. twitch channel, patreon campaign, youtube channel, BuyMeACoffee page, etc.
/// </summary>
public class SupportTargetEntity
{
    public Guid Id { get; set; }

    public CreatorEntity Creator { get; set; } = null!;
    public Guid CreatorId { get; set; }

    /// <summary>
    /// The staff member who set up the support target.
    /// Prevents staff member from being removed from the creator.
    /// </summary>
    public ApplicationUser OwningStaffMember { get; set; } = null!;
    public Guid OwningStaffMemberId { get; set; }

    public CreatorStaffEntity StaffEntry { get; set; } = null!;
    
    public required SupportServiceType ServiceType { get; set; }
    
    [MaxLength(150)]
    public required string ServiceId { get; set; }

    public ThirdPartyToken? ServiceToken { get; set; }
    public long? ServiceTokenId { get; set; }
}

internal class SupportTargetEntityConfiguration : IEntityTypeConfiguration<SupportTargetEntity>
{
    public void Configure(EntityTypeBuilder<SupportTargetEntity> builder)
    {
        // Do not permit multiple creators to share same donations
        builder.HasAlternateKey(x => new { x.ServiceType, x.ServiceId });
        
        // Do not permit creator connected to more than one "profile" per service
        // Otherwise there is a possibility of different creators using same account
        // and thus not being charged separately
        builder.HasAlternateKey(x => new { x.CreatorId, x.ServiceType });
        
        builder.HasOne(x => x.StaffEntry)
            .WithMany(x => x.OwnedSupportTargets)
            .HasForeignKey(x => new { x.CreatorId, x.OwningStaffMemberId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

