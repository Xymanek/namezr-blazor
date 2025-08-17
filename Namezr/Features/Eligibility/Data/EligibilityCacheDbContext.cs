using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Namezr.Features.Eligibility.Data;

public class EligibilityCacheDbContext : DbContext
{
    public EligibilityCacheDbContext(DbContextOptions<EligibilityCacheDbContext> options) : base(options)
    {
    }
    
    public DbSet<EligibilityCacheEntity> CacheEntries { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<EligibilityCacheEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.ExpiresAt)
                .HasDatabaseName("IX_EligibilityCacheEntity_ExpiresAt");
            
            entity.HasIndex(e => new { e.UserId, e.EligibilityConfigurationId })
                .HasDatabaseName("IX_EligibilityCacheEntity_UserId_EligibilityConfigurationId");
            
            entity.Property(e => e.ExpiresAt)
                .HasConversion(
                    instant => instant.ToUnixTimeSeconds(),
                    unixTime => Instant.FromUnixTimeSeconds(unixTime)
                );
        });
    }
}