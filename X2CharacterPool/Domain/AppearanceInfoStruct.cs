using X2CharacterPool.PropertyNodes;

namespace X2CharacterPool.Domain;

public record AppearanceInfoStruct
{
    public required string GenderArmorTemplate { get; init; }
    public required AppearanceStruct Appearance { get; init; }

    public static AppearanceInfoStruct Load(ArrayEntry arrayEntry)
    {
        Dictionary<string, IProperty> properties = arrayEntry.Properties.ToDictionary(property => property.Header.Name);

        return new AppearanceInfoStruct
        {
            GenderArmorTemplate = PropertyHelper.GetStringProperty(properties, "GenderArmorTemplate"),
            Appearance = AppearanceStruct.Load(
                arrayEntry.Properties
                    .OfType<StructProperty>()
                    .Single(structProp => structProp.Header.Name == "Appearance")
                ),
        };
    }
}