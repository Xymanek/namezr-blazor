using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;

namespace Namezr.Features.Creators.Data;

public class StaffInviteEntity
{
    // Make sure this is a "fully random" GUID, not UUIDv7 which is used by npgsql by default
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; init; } = Guid.NewGuid();

    public CreatorEntity Creator { get; set; } = null!;
    public Guid CreatorId { get; set; }

    public Instant CreatedAt { get; set; } = SystemClock.Instance.GetCurrentInstant();
}