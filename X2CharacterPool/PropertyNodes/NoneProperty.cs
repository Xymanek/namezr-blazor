namespace X2CharacterPool.PropertyNodes;

public record NoneProperty : IPropertyHeader, IProperty
{
    public const string TypeName = "None";
    string IPropertyHeader.TypeName => TypeName;

    public string Name => "None";

    public IPropertyHeader Header => this;

    object IProperty.Value => null!;
}