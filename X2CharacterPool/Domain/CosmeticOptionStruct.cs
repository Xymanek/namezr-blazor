using X2CharacterPool.PropertyNodes;

namespace X2CharacterPool.Domain;

public record CosmeticOptionStruct
{
    public required string OptionName { get; init; }
    public required bool IsChecked { get; init; }

    public static CosmeticOptionStruct Load(ArrayEntry entry)
    {
        string optionName = string.Empty;
        bool isChecked = false;

        foreach (IProperty property in entry.Properties)
        {
            switch (property)
            {
                case StringProperty { Name: "OptionName" } stringProperty:
                    optionName = stringProperty.Value;
                    break;

                case BoolProperty { Name: "bChecked" } boolProperty:
                    isChecked = boolProperty.Value;
                    break;
            }
        }

        return new CosmeticOptionStruct
        {
            OptionName = optionName,
            IsChecked = isChecked
        };
    }
}