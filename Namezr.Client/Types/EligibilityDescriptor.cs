﻿using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Namezr.Client.Types;

// ReSharper disable once NotAccessedPositionalProperty.Global
public record EligibilityDescriptor(EligibilityDescriptorData Data)
{
    [JsonIgnore]
    public EligibilityId Id { get; } = Data.BuildId();
}

public record EligibilityDescriptorData
{
    public EligibilityType Type { get; }

    public SupportPlan? SupportPlan { get; }
    public VirtualEligibilityType? VirtualEligibilityType { get; }
    
    public EligibilityDescriptorData(SupportPlan supportPlan)
    {
        Type = EligibilityType.SupportPlan;
        SupportPlan = supportPlan;
    }

    public EligibilityDescriptorData(VirtualEligibilityType virtualEligibilityType)
    {
        Type = EligibilityType.Virtual;
        VirtualEligibilityType = virtualEligibilityType;
    }

    [JsonConstructor]
    private EligibilityDescriptorData(
        EligibilityType type, 
        SupportPlan? supportPlan, 
        VirtualEligibilityType? virtualEligibilityType
    )
    {
        if (
            type == EligibilityType.SupportPlan != supportPlan is not null ||
            type == EligibilityType.Virtual != virtualEligibilityType is not null
        )
        {
            throw new ArgumentException("Type and support plan/virtual eligibility type must match");
        }
        
        Type = type;
        SupportPlan = supportPlan;
        VirtualEligibilityType = virtualEligibilityType;
    }

    public EligibilityId BuildId()
    {
        return new EligibilityId(DoBuildIdChain());
    }

    private IEnumerable<string> DoBuildIdChain()
    {
        yield return ((int)Type).ToString();

        switch (Type)
        {
            case EligibilityType.SupportPlan:
                yield return SupportPlan!.SupportTargetId.ToString();
                yield return SupportPlan!.SupportPlanId;
                break;

            case EligibilityType.Virtual:
                yield return ((int)VirtualEligibilityType!.Value).ToString();
                break;

            default:
                throw new UnreachableException();
        }
    }
}

public readonly record struct EligibilityId
{
    public IReadOnlyList<string> Chain { get; }

    public EligibilityId(IEnumerable<string> chain)
    {
        Chain = chain.ToArray();
    }

    public bool Equals(EligibilityId other)
    {
        return Chain.SequenceEqual(other.Chain);
    }

    public override int GetHashCode()
    {
        HashCode hashCode = new();

        foreach (string part in Chain)
        {
            hashCode.Add(part);
        }

        return hashCode.ToHashCode();
    }
}