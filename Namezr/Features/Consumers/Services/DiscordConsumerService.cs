using AspNet.Security.OAuth.Discord;
using EntityFramework.Exceptions.Common;
using Microsoft.EntityFrameworkCore;
using Namezr.Client.Types;
using Namezr.Features.Consumers.Data;
using Namezr.Features.Creators.Data;
using Namezr.Features.Identity.Data;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Consumers.Services;

// TODO: rename to DiscordConsumerManager or something such

[AutoConstructor]
[RegisterSingleton]
public partial class DiscordConsumerService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public async Task<TargetConsumerEntity> GetOrCreateDiscordConsumer(Guid userId, Guid creatorId)
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        SupportTargetEntity supportTarget = await dbContext.SupportTargets
            .SingleAsync(x => x.Id == creatorId && x.ServiceType == SupportServiceType.Discord);

        ApplicationUserLogin userLogin = await dbContext.UserLogins
            .SingleAsync(x =>
                x.UserId == userId &&
                x.LoginProvider == DiscordAuthenticationDefaults.AuthenticationScheme
            );

        return await GetOrCreateDiscordConsumer(userLogin, supportTarget.Id);
    }

    public async Task<TargetConsumerEntity> GetOrCreateDiscordConsumer(
        ApplicationUserLogin userLogin,
        Guid supportTargetId
    )
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        TargetConsumerEntity? entity = await dbContext.TargetConsumers
            .SingleOrDefaultAsync(x =>
                x.ServiceUserId == userLogin.ProviderKey && x.SupportTargetId == supportTargetId
            );

        if (entity is not null)
        {
            return entity;
        }

        try
        {
            entity = new TargetConsumerEntity
            {
                SupportTargetId = supportTargetId,
                ServiceUserId = userLogin.ProviderKey,
            };

            dbContext.TargetConsumers.Add(entity);
            await dbContext.SaveChangesAsync();

            return entity;
        }
        catch (UniqueConstraintException)
        {
            // Race, it was already created

            return await dbContext.TargetConsumers
                .SingleAsync(x => x.ServiceUserId == userLogin.ProviderKey && x.SupportTargetId == supportTargetId);
        }
    }
}