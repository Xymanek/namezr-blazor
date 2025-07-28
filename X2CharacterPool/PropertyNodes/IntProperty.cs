namespace X2CharacterPool.PropertyNodes;

public record IntProperty : ISimpleProperty<int>
{
    public const string TypeName = "IntProperty";
    string IPropertyHeader.TypeName => TypeName;

    public static int DefaultValue => 0;

    public required string Name { get; init; }

    public required int Value { get; init; }
    object IProperty.Value => Value;
}