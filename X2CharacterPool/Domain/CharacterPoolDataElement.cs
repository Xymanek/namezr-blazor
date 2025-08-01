using X2CharacterPool.PropertyNodes;

namespace X2CharacterPool.Domain;

public record CharacterPoolDataElement
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string NickName { get; init; }
    public required string SoldierClassTemplateName { get; init; }
    public required string CharacterTemplateName { get; init; }
    public required string Country { get; init; }
    public required bool AllowedTypeSoldier { get; init; }
    public required bool AllowedTypeVIP { get; init; }
    public required bool AllowedTypeDarkVIP { get; init; }
    public required string PoolTimestamp { get; init; }
    public required string BackgroundText { get; init; }

    public static CharacterPoolDataElement Load(StructProperty @struct)
    {
        Dictionary<string, IProperty> properties = @struct.Value.ToDictionary(p => p.Header.Name);

        return Load(properties);
    }

    public static CharacterPoolDataElement Load(ArrayEntry arrayEntry)
    {
        Dictionary<string, IProperty> properties = arrayEntry.Properties.ToDictionary(p => p.Header.Name);

        return Load(properties);
    }

    private static CharacterPoolDataElement Load(IReadOnlyDictionary<string, IProperty> properties)
    {
        return new CharacterPoolDataElement
        {
            FirstName = PropertyHelper.GetStringProperty(properties, "strFirstName"),
            LastName = PropertyHelper.GetStringProperty(properties, "strLastName"),
            NickName = PropertyHelper.GetStringProperty(properties, "strNickName"),
            SoldierClassTemplateName = PropertyHelper.GetNameProperty(properties, "m_SoldierClassTemplateName"),
            CharacterTemplateName = PropertyHelper.GetNameProperty(properties, "CharacterTemplateName"),
            Country = PropertyHelper.GetNameProperty(properties, "Country"),
            AllowedTypeSoldier = PropertyHelper.GetBoolProperty(properties, "AllowedTypeSoldier"),
            AllowedTypeVIP = PropertyHelper.GetBoolProperty(properties, "AllowedTypeVIP"),
            AllowedTypeDarkVIP = PropertyHelper.GetBoolProperty(properties, "AllowedTypeDarkVIP"),
            PoolTimestamp = PropertyHelper.GetStringProperty(properties, "PoolTimestamp"),
            BackgroundText = PropertyHelper.GetStringProperty(properties, "BackgroundText"),
        };
    }
}