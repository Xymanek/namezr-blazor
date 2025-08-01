using Microsoft.EntityFrameworkCore;
using Namezr.Features.Questionnaires.Data;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Questionnaires.Services;

internal interface IAttributeUpdaterService
{
    /// <summary>
    /// Updates the attribute in the database and saves changes.
    /// </summary>
    Task UpdateAttributeAsync(
        AttributeUpdateCommand command,
        CancellationToken ct
    );

    /// <summary>
    /// Prepares the update in <paramref name="dbContext"/> but does not save changes.
    /// </summary>
    Task StageAttributeUpdateAsync(
        AttributeUpdateCommand command,
        ApplicationDbContext dbContext,
        CancellationToken ct
    );
}

[AutoConstructor]
[RegisterSingleton]
internal partial class AttributeUpdaterService : IAttributeUpdaterService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly ISubmissionAuditService _auditService;

    public async Task UpdateAttributeAsync(
        AttributeUpdateCommand command,
        CancellationToken ct
    )
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        await StageAttributeUpdateAsync(command, dbContext, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task StageAttributeUpdateAsync(
        AttributeUpdateCommand command,
        ApplicationDbContext dbContext,
        CancellationToken ct
    )
    {
        // Normalize the key
        command = command with
        {
            Key = command.Key.Trim(),
        };

        // First try to find the attribute amongst the currently staged changes
        // (e.g. add and then add again)
        SubmissionAttributeEntity? existingAttribute = dbContext.SubmissionAttributes.Local
            .SingleOrDefault(attr =>
                attr.SubmissionId == command.SubmissionId &&
                attr.Key.Equals(command.Key, StringComparison.OrdinalIgnoreCase)
            );

        // If not found, try to find an existing DB record
        if (existingAttribute == null)
        {
            existingAttribute = await dbContext.SubmissionAttributes
                .AsTracking()
                .SingleOrDefaultAsync(
                    attr =>
                        attr.SubmissionId == command.SubmissionId &&
                        attr.Key.ToLower() == command.Key.ToLower(),
                    ct
                );
        }

        if (string.IsNullOrEmpty(command.Value))
        {
            // Delete attribute if value is empty
            if (existingAttribute != null)
            {
                dbContext.SubmissionAttributes.Remove(existingAttribute);
                AuditDeletion(dbContext, command, existingAttribute);
            }
        }
        else
        {
            if (existingAttribute != null)
            {
                // Update existing attribute
                existingAttribute.Value = command.Value;

                // Log update
                AuditUpdate(dbContext, command, existingAttribute);
            }
            else
            {
                SubmissionAttributeEntity newAttribute = new()
                {
                    SubmissionId = command.SubmissionId,
                    Key = command.Key,
                    Value = command.Value,
                };

                dbContext.SubmissionAttributes.Add(newAttribute);
                AuditCreation(dbContext, command, newAttribute);
            }
        }
    }

    private void AuditCreation(
        ApplicationDbContext dbContext,
        AttributeUpdateCommand command,
        SubmissionAttributeEntity newAttribute
    )
    {
        SubmissionHistoryAttributeUpdatedEntity audit;
        if (command.InstigatorIsProgrammatic)
        {
            audit = _auditService.AttributeUpdatedProgrammatic(
                command.SubmissionId, newAttribute.Key, newAttribute.Value,
                null
            );
        }
        else
        {
            audit = _auditService.AttributeUpdated(
                command.SubmissionId,
                newAttribute.Key,
                value: newAttribute.Value,
                previousValue: null
            );
        }

        dbContext.SubmissionHistoryEntries.Add(audit);
    }

    private void AuditUpdate(
        ApplicationDbContext dbContext,
        AttributeUpdateCommand command,
        SubmissionAttributeEntity existingAttribute
    )
    {
        string previousValue = existingAttribute.Value;

        SubmissionHistoryAttributeUpdatedEntity audit;
        if (command.InstigatorIsProgrammatic)
        {
            audit = _auditService.AttributeUpdatedProgrammatic(
                command.SubmissionId,
                command.Key,
                command.Value,
                previousValue
            );
        }
        else
        {
            audit = _auditService.AttributeUpdated(
                command.SubmissionId,
                command.Key,
                command.Value,
                previousValue
            );
        }

        dbContext.SubmissionHistoryEntries.Add(audit);
    }

    private void AuditDeletion(
        ApplicationDbContext dbContext,
        AttributeUpdateCommand command,
        SubmissionAttributeEntity existingAttribute
    )
    {
        SubmissionHistoryAttributeUpdatedEntity audit;
        if (command.InstigatorIsProgrammatic)
        {
            audit = _auditService.AttributeDeletedProgrammatic(
                command.SubmissionId,
                command.Key,
                existingAttribute.Value
            );
        }
        else
        {
            audit = _auditService.AttributeDeleted(
                command.SubmissionId,
                command.Key,
                existingAttribute.Value
            );
        }

        dbContext.SubmissionHistoryEntries.Add(audit);
    }
}

public record AttributeUpdateCommand
{
    public required Guid SubmissionId { get; init; }

    public required string Key { get; init; }
    public required string Value { get; init; }

    public required bool InstigatorIsProgrammatic { get; init; }
}