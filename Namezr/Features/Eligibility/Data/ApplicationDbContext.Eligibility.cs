using Microsoft.EntityFrameworkCore;
using Namezr.Features.Eligibility.Data;

// ReSharper disable once CheckNamespace
namespace Namezr.Infrastructure.Data;

public partial class ApplicationDbContext
{
    public DbSet<EligibilityConfigurationEntity> EligibilityConfigurations { get; set; } = null!;
    public DbSet<EligibilityOptionEntity> EligibilityOptions { get; set; } = null!;
}