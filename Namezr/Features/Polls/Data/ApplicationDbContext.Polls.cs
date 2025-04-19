using Namezr.Features.Polls.Data;
using Microsoft.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Namezr.Infrastructure.Data;

public partial class ApplicationDbContext
{
    public DbSet<PollEntity> Polls { get; set; } = null!;
    public DbSet<PollOptionEntity> PollOptions { get; set; } = null!;
    public DbSet<PollChoiceEntity> PollChoices { get; set; } = null!;
}