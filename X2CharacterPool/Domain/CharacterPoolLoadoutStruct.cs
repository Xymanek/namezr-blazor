using X2CharacterPool.PropertyNodes;

namespace X2CharacterPool.Domain;

public record CharacterPoolLoadoutStruct
{
    public required string TemplateName { get; init; }
    public required string InventorySlot { get; init; }

    public static CharacterPoolLoadoutStruct Load(ArrayEntry entry)
    {
        string templateName = string.Empty;
        string inventorySlot = string.Empty;

        foreach (IProperty property in entry.Properties)
        {
            switch (property)
            {
                case StringProperty { Name: "TemplateName" } stringProperty:
                    templateName = stringProperty.Value;
                    break;

                case StringProperty { Name: "InventorySlot" } slotProperty:
                    inventorySlot = slotProperty.Value;
                    break;
            }
        }

        return new CharacterPoolLoadoutStruct
        {
            TemplateName = templateName,
            InventorySlot = inventorySlot
        };
    }
}