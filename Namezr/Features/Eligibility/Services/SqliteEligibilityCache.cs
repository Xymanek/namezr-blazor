using AutoRegisterInject;
using Microsoft.EntityFrameworkCore;
using Namezr.Features.Eligibility.Data;
using NodaTime;

namespace Namezr.Features.Eligibility.Services;

public interface IEligibilityCache
{
    Task<EligibilityResult?> GetAsync(Guid userId, EligibilityConfigurationEntity configuration);
    Task SetAsync(Guid userId, EligibilityConfigurationEntity configuration, EligibilityResult result, TimeSpan expiration);
    Task CleanupExpiredEntriesAsync();
}

[RegisterSingleton]
internal class SqliteEligibilityCache : IEligibilityCache
{
    private readonly IDbContextFactory<EligibilityCacheDbContext> _dbContextFactory;
    private readonly IClock _clock;

    public SqliteEligibilityCache(
        IDbContextFactory<EligibilityCacheDbContext> dbContextFactory,
        IClock clock)
    {
        _dbContextFactory = dbContextFactory;
        _clock = clock;
    }

    public async Task<EligibilityResult?> GetAsync(Guid userId, EligibilityConfigurationEntity configuration)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        
        string cacheKey = EligibilityCacheEntity.GenerateId(userId, configuration.Id);
        Instant now = _clock.GetCurrentInstant();
        
        var entry = await dbContext.CacheEntries
            .Where(e => e.Id == cacheKey && e.ExpiresAt > now)
            .FirstOrDefaultAsync();
            
        return entry?.DeserializeResult();
    }

    public async Task SetAsync(Guid userId, EligibilityConfigurationEntity configuration, EligibilityResult result, TimeSpan expiration)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        
        string cacheKey = EligibilityCacheEntity.GenerateId(userId, configuration.Id);
        Instant expiresAt = _clock.GetCurrentInstant() + Duration.FromTimeSpan(expiration);
        
        var entry = await dbContext.CacheEntries
            .Where(e => e.Id == cacheKey)
            .FirstOrDefaultAsync();
            
        if (entry != null)
        {
            entry.SerializeResult(result);
            entry.ExpiresAt = expiresAt;
        }
        else
        {
            entry = new EligibilityCacheEntity
            {
                Id = cacheKey,
                UserId = userId,
                EligibilityConfigurationId = configuration.Id,
                ExpiresAt = expiresAt,
            };
            entry.SerializeResult(result);
            
            dbContext.CacheEntries.Add(entry);
        }
        
        await dbContext.SaveChangesAsync();
    }

    public async Task CleanupExpiredEntriesAsync()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        
        Instant now = _clock.GetCurrentInstant();
        
        await dbContext.CacheEntries
            .Where(e => e.ExpiresAt <= now)
            .ExecuteDeleteAsync();
    }
}