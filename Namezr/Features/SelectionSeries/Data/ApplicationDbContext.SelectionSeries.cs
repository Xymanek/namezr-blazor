using Microsoft.EntityFrameworkCore;
using Namezr.Features.SelectionSeries.Data;

// ReSharper disable once CheckNamespace
namespace Namezr.Infrastructure.Data;

public partial class ApplicationDbContext
{
    public DbSet<SelectionSeriesEntity> SelectionSeries { get; set; } = null!;

    public DbSet<SelectionBatchEntity> SelectionBatches { get; set; } = null!;
    public DbSet<SelectionEntryEntity> SelectionEntries { get; set; } = null!;
    public DbSet<SelectionEventEntity> SelectionEvents { get; set; } = null!;

    public DbSet<SelectionUserDataEntity> SelectionUserData { get; set; } = null!;
    
    public DbSet<SelectionCandidateEntity> SelectionCandidates { get; set; } = null!;
}