using AspNet.Security.OAuth.Patreon;
using EntityFramework.Exceptions.Common;
using Microsoft.EntityFrameworkCore;
using Namezr.Client.Types;
using Namezr.Features.Consumers.Data;
using Namezr.Features.Creators.Data;
using Namezr.Features.Identity.Data;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Consumers.Services;

// TODO: rename to PatreonConsumerManager or something such

[AutoConstructor]
[RegisterSingleton]
public partial class PatreonConsumerService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public async Task<TargetConsumerEntity> GetOrCreatePatreonConsumer(Guid userId, Guid creatorId)
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        SupportTargetEntity supportTarget = await dbContext.SupportTargets
            .SingleAsync(x => x.Id == creatorId && x.ServiceType == SupportServiceType.Patreon);

        ApplicationUserLogin userLogin = await dbContext.UserLogins
            .SingleAsync(x =>
                x.UserId == userId &&
                x.LoginProvider == PatreonAuthenticationDefaults.AuthenticationScheme
            );

        return await GetOrCreatePatreonConsumer(userLogin, supportTarget.Id);
    }

    public async Task<TargetConsumerEntity> GetOrCreatePatreonConsumer(
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