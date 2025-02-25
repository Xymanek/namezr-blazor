using AspNet.Security.OAuth.Twitch;
using EntityFramework.Exceptions.Common;
using Microsoft.EntityFrameworkCore;
using Namezr.Client.Types;
using Namezr.Features.Consumers.Data;
using Namezr.Features.Creators.Data;
using Namezr.Features.Identity.Data;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Consumers.Services;

// TODO: rename to TwitchConsumerManager or something such

[AutoConstructor]
[RegisterSingleton]
public partial class TwitchConsumerService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public async Task<TargetConsumerEntity> GetOrCreateTwitchConsumer(Guid userId, Guid creatorId)
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        SupportTargetEntity supportTarget = await dbContext.SupportTargets
            .SingleAsync(x => x.Id == creatorId && x.ServiceType == SupportServiceType.Twitch);

        ApplicationUserLogin userLogin = await dbContext.UserLogins
            .SingleAsync(x =>
                x.UserId == userId &&
                x.LoginProvider == TwitchAuthenticationDefaults.AuthenticationScheme
            );

        return await GetOrCreateTwitchConsumer(userLogin, supportTarget.Id);
    }

    public async Task<TargetConsumerEntity> GetOrCreateTwitchConsumer(
        ApplicationUserLogin userLogin,
        Guid supportTargetId
    )
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        TargetConsumerEntity? entity = await dbContext.TargetConsumers
            .SingleOrDefaultAsync(x => x.ServiceId == userLogin.ProviderKey && x.SupportTargetId == supportTargetId);

        if (entity is not null)
        {
            return entity;
        }

        try
        {
            entity = new TargetConsumerEntity
            {
                SupportTargetId = supportTargetId,
                ServiceId = userLogin.ProviderKey,
            };

            dbContext.TargetConsumers.Add(entity);
            await dbContext.SaveChangesAsync();

            return entity;
        }
        catch (UniqueConstraintException)
        {
            // Race, it was already created

            return await dbContext.TargetConsumers
                .SingleAsync(x => x.ServiceId == userLogin.ProviderKey && x.SupportTargetId == supportTargetId);
        }
    }
}