namespace X2CharacterPool.PropertyNodes;

public record NameProperty : ISimpleProperty<string>
{
    public const string TypeName = "NameProperty";
    string IPropertyHeader.TypeName => TypeName;

    public static string DefaultValue => string.Empty;

    public required string Name { get; init; }

    public required string Value { get; init; }
    object IProperty.Value => Value;
}