using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Namezr.Features.ThirdParty.Data;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.ThirdParty.Cli;

/// <summary>
/// Use only for debugging (retrieving the decoded token value).
/// </summary>
[AutoConstructor]
[RegisterSingleton]
public partial class PrintThirdPartyTokenCommand
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public async Task Execute(IEnumerable<string> args)
    {
        string tokenIdStr = args.First();
        long tokenId = long.Parse(tokenIdStr);

        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        ThirdPartyToken token = await dbContext.ThirdPartyTokens
            .SingleAsync(x => x.Id == tokenId);

        Console.WriteLine($"ID: {token.Id}");
        Console.WriteLine($"ServiceType: {token.ServiceType}");
        Console.WriteLine($"ServiceAccountId: {token.ServiceAccountId}");
        Console.WriteLine($"TokenType: {token.TokenType}");

        Console.WriteLine("Value: " + JsonSerializer.Serialize(token.Value, JsonSerializerOptions));
        Console.WriteLine("Context: " + JsonSerializer.Serialize(token.Context, JsonSerializerOptions));
    }
}