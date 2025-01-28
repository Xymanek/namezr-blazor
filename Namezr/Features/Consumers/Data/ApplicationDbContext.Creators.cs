using Microsoft.EntityFrameworkCore;
using Namezr.Features.Consumers.Data;

// ReSharper disable once CheckNamespace
namespace Namezr.Infrastructure.Data;

public partial class ApplicationDbContext
{
    public DbSet<TargetConsumerEntity> TargetConsumers { get; set; } = null!;
    public DbSet<ConsumerSupportStatusEntity> ConsumerSupportStatus { get; set; } = null!;
}
