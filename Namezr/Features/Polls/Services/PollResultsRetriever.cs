using Microsoft.EntityFrameworkCore;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Polls.Services;

public interface IPollResultsRetriever
{
    /// <summary>
    /// Important: an option can be missing from the result if it was not voted in the poll.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, PollOptionResult>> CalculatePerOptionStats(Guid pollId);
}

public record PollOptionResult
{
    public required Guid OptionId { get; init; }

    public required int VotesCount { get; init; }
    public required decimal OptionWeight { get; init; }

    /// <summary>
    /// <c>0</c> - no votes. <c>1</c> - all votes.
    /// </summary>
    public required decimal WeightedRatio { get; init; }
}

[AutoConstructor]
[RegisterSingleton]
public partial class PollResultsRetriever : IPollResultsRetriever
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public async Task<IReadOnlyDictionary<Guid, PollOptionResult>> CalculatePerOptionStats(Guid pollId)
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        var optionStats = await dbContext.PollChoices
            .Where(x => x.PollId == pollId)
            .GroupBy(x => x.OptionId)
            .Select(group => new
            {
                OptionId = group.Key,

                VotesCount = group.Count(),
                OptionWeight = group.Sum(choice => choice.Weight),
            })
            .ToArrayAsync();

        decimal globalWeight = optionStats.Sum(x => x.OptionWeight);

        return optionStats
            .OrderBy(arg => arg.OptionId) // Consistent ordering, mainly for debugging
            .ToDictionary(arg => arg.OptionId, arg => new PollOptionResult
            {
                OptionId = arg.OptionId,
                VotesCount = arg.VotesCount,
                OptionWeight = arg.OptionWeight,
                WeightedRatio = arg.OptionWeight / globalWeight,
            });
    }
}