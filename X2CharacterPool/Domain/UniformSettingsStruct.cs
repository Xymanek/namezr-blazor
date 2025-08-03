using System.Collections.Immutable;
using X2CharacterPool.PropertyNodes;
using X2CharacterPool.Serialization;

namespace X2CharacterPool.Domain;

public record UniformSettingsStruct
{
    public required string GenderArmorTemplate { get; init; }
    public required ImmutableList<CosmeticOptionStruct> CosmeticOptions { get; init; }

    public static UniformSettingsStruct Load(ArrayEntry entry)
    {
        return new UniformSettingsStruct
        {
            GenderArmorTemplate = entry.Properties
                .OfType<StringProperty>()
                .Single(property => property is { Name: "GenderArmorTemplate" })
                .Value,
            
            CosmeticOptions = entry.Properties
                .OfType<ArrayProperty>()
                .Single(property => property is { Header.Name: "CosmeticOptions" })
                .Value
                .Select(CosmeticOptionStruct.Load)
                .ToImmutableList()
        };
    }
}