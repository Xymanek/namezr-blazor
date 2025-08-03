using System.Collections.Immutable;
using X2CharacterPool.PropertyNodes;

namespace X2CharacterPool.Domain;

public record AppearanceStruct
{
    // Based on the UnrealScript TAppearance structure

    // Will suffice for now
    public required ImmutableDictionary<string, string> Values { get; init; }

    public static AppearanceStruct Load(StructProperty @struct)
    {
        return new AppearanceStruct
        {
            Values = @struct.Value
                .Select(property => KeyValuePair.Create(
                    property.Header.Name,
                    property.Value.ToString() ?? StringProperty.DefaultValue
                ))
                .ToImmutableDictionary(),
        };
    }
}