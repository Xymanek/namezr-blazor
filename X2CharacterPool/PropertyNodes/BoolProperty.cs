namespace X2CharacterPool.PropertyNodes;

public record BoolProperty : ISimpleProperty<bool>
{
    public const string TypeName = "BoolProperty";
    string IPropertyHeader.TypeName => TypeName;

    public static bool DefaultValue => false;

    public required string Name { get; init; }

    public required bool Value { get; init; }
    object IProperty.Value => Value;
}