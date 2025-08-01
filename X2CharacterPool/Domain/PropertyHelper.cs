using System.Collections.Immutable;
using X2CharacterPool.PropertyNodes;

namespace X2CharacterPool.Domain;

public static class PropertyHelper
{
    public static string GetStringProperty(IReadOnlyDictionary<string, IProperty> properties, string key)
    {
        return properties.TryGetValue(key, out var property) && property is StringProperty stringProp 
            ? stringProp.Value 
            : StringProperty.DefaultValue;
    }

    public static string GetNameProperty(IReadOnlyDictionary<string, IProperty> properties, string key)
    {
        return properties.TryGetValue(key, out var property) && property is NameProperty nameProp 
            ? nameProp.Value 
            : NameProperty.DefaultValue;
    }

    public static bool GetBoolProperty(IReadOnlyDictionary<string, IProperty> properties, string key)
    {
        return properties.TryGetValue(key, out var property) && property is BoolProperty boolProp 
            ? boolProp.Value 
            : BoolProperty.DefaultValue;
    }

    public static int GetIntProperty(ImmutableList<IProperty> properties, string name)
    {
        return properties
            .OfType<IntProperty>()
            .FirstOrDefault(property => property.Name == name)
            ?.Value ?? IntProperty.DefaultValue;
    }

    public static CharacterPoolDataElement GetStructProperty(ImmutableList<IProperty> properties, string name)
    {
        var structProp = properties
            .OfType<StructProperty>()
            .FirstOrDefault(property => property.Header.Name == name);
        
        return structProp != null 
            ? CharacterPoolDataElement.Load(structProp)
            : new CharacterPoolDataElement
            {
                FirstName = StringProperty.DefaultValue,
                LastName = StringProperty.DefaultValue,
                NickName = StringProperty.DefaultValue,
                SoldierClassTemplateName = NameProperty.DefaultValue,
                CharacterTemplateName = NameProperty.DefaultValue,
                Country = NameProperty.DefaultValue,
                AllowedTypeSoldier = BoolProperty.DefaultValue,
                AllowedTypeVIP = BoolProperty.DefaultValue,
                AllowedTypeDarkVIP = BoolProperty.DefaultValue,
                PoolTimestamp = StringProperty.DefaultValue,
                BackgroundText = StringProperty.DefaultValue
            };
    }

    public static ImmutableList<UniformSettingsStruct> GetUniformSettings(ImmutableList<IProperty> properties)
    {
        var arrayProp = properties
            .OfType<ArrayProperty>()
            .FirstOrDefault(property => property.Header.Name == "UniformSettings");
        
        return arrayProp?.Value
            .Select(UniformSettingsStruct.Load)
            .ToImmutableList() ?? [];
    }

    public static T GetEnumProperty<T>(ImmutableList<IProperty> properties, string name) where T : Enum
    {
        int intValue = GetIntProperty(properties, name);
        return (T)Enum.ToObject(typeof(T), intValue);
    }
}