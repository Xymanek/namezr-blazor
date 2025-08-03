using System.Collections.Immutable;
using X2CharacterPool.PropertyNodes;

namespace X2CharacterPool.Domain;

public record ExtraDataEntry
{
    public required int ObjectId { get; init; }
    public required CharacterPoolDataElement CharPoolData { get; init; }
    public required ImmutableList<AppearanceInfoStruct> AppearanceStore { get; init; }
    public required ImmutableList<UniformSettingsStruct> UniformSettings { get; init; }
    public required EUniformStatus UniformStatus { get; init; }
    public required EAutoManageUniformForUnit AutoManageUniformForUnit { get; init; }
    public required ImmutableList<string> NonSoldierUniformTemplates { get; init; }
    public required ImmutableList<CharacterPoolLoadoutStruct> CharacterPoolLoadout { get; init; }

    public static ExtraDataEntry Load(ArrayEntry serializedEntry)
    {
        return new ExtraDataEntry
        {
            ObjectId = PropertyHelper.GetIntProperty(serializedEntry.Properties, "ObjectID"),
            CharPoolData = PropertyHelper.GetStructProperty(serializedEntry.Properties, "CharPoolData"),
            AppearanceStore = GetAppearanceStore(serializedEntry.Properties),
            UniformSettings = PropertyHelper.GetUniformSettings(serializedEntry.Properties),
            UniformStatus = PropertyHelper.GetEnumProperty<EUniformStatus>(serializedEntry.Properties, "UniformStatus"),
            AutoManageUniformForUnit = PropertyHelper.GetEnumProperty<EAutoManageUniformForUnit>(serializedEntry.Properties, "AutoManageUniformForUnit"),
            NonSoldierUniformTemplates = GetNonSoldierUniformTemplates(serializedEntry.Properties),
            CharacterPoolLoadout = GetCharacterPoolLoadout(serializedEntry.Properties)
        };
    }

    private static ImmutableList<AppearanceInfoStruct> GetAppearanceStore(ImmutableList<IProperty> properties)
    {
        var arrayProp = properties
            .OfType<ArrayProperty>()
            .FirstOrDefault(property => property.Header.Name == "AppearanceStore");
        
        return arrayProp?.Value
            .Select(AppearanceInfoStruct.Load)
            .ToImmutableList() ?? [];
    }

    private static ImmutableList<string> GetNonSoldierUniformTemplates(ImmutableList<IProperty> properties)
    {
        var arrayProp = properties
            .OfType<ArrayProperty>()
            .FirstOrDefault(property => property.Header.Name == "NonSoldierUniformTemplates");
        
        if (arrayProp?.Value.FirstOrDefault() is { } firstEntry)
        {
            return firstEntry.Properties
                .Cast<StringProperty>()
                .Select(p => p.Value)
                .ToImmutableList();
        }
        
        return [];
    }

    private static ImmutableList<CharacterPoolLoadoutStruct> GetCharacterPoolLoadout(ImmutableList<IProperty> properties)
    {
        var arrayProp = properties
            .OfType<ArrayProperty>()
            .FirstOrDefault(property => property.Header.Name == "CharacterPoolLoadout");
        
        return arrayProp?.Value
            .Select(CharacterPoolLoadoutStruct.Load)
            .ToImmutableList() ?? [];
    }
}