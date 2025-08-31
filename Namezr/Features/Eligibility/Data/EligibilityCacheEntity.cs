using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Namezr.Client.Types;
using Namezr.Features.Eligibility.Services;
using NodaTime;

namespace Namezr.Features.Eligibility.Data;

public class EligibilityCacheEntity
{
    [Key]
    public string Id { get; set; } = null!;
    
    public Guid UserId { get; set; }
    
    public long EligibilityConfigurationId { get; set; }
    
    public string SerializedResult { get; set; } = null!;
    
    public Instant ExpiresAt { get; set; }
    
    public static string GenerateId(Guid userId, long eligibilityConfigurationId)
    {
        return $"{userId}:{eligibilityConfigurationId}";
    }
    
    public EligibilityResult DeserializeResult()
    {
        var data = JsonSerializer.Deserialize<SerializedEligibilityResult>(SerializedResult)!;
        
        return new EligibilityResult
        {
            EligiblePlanIds = data.EligiblePlanIds.ToImmutableHashSet(),
            Modifier = data.Modifier,
            MaxSubmissionsPerUser = data.MaxSubmissionsPerUser,
        };
    }
    
    public void SerializeResult(EligibilityResult result)
    {
        var data = new SerializedEligibilityResult
        {
            EligiblePlanIds = result.EligiblePlanIds.ToArray(),
            Modifier = result.Modifier,
            MaxSubmissionsPerUser = result.MaxSubmissionsPerUser,
        };
        
        SerializedResult = JsonSerializer.Serialize(data);
    }
    
    private class SerializedEligibilityResult
    {
        public EligibilityPlanId[] EligiblePlanIds { get; set; } = null!;
        public decimal Modifier { get; set; }
        public int MaxSubmissionsPerUser { get; set; }
    }
}