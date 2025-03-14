using Microsoft.EntityFrameworkCore;
using Namezr.Features.Creators.Data;

// ReSharper disable once CheckNamespace
namespace Namezr.Infrastructure.Data;

public partial class ApplicationDbContext
{
    public DbSet<CreatorEntity>  Creators { get; set; } = null!;
    public DbSet<CreatorStaffEntity> CreatorStaff { get; set; } = null!;
    public DbSet<SupportPlanInfoEntity> SupportPlans { get; set; } = null!; // TODO: rename to SupportPlanInfos
    public DbSet<SupportTargetEntity> SupportTargets { get; set; } = null!;
    
    public DbSet<StaffInviteEntity> StaffInvites { get; set; } = null!;
}
