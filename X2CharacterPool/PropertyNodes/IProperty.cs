namespace X2CharacterPool.PropertyNodes;

public interface IProperty
{
    IPropertyHeader Header { get; }
    
    object Value { get; }
}

public interface IProperty<out TValue> : IProperty
{
    new TValue Value { get; }
}

public interface ISimpleProperty<out TValue> : IProperty<TValue>, IPropertyHeader
{
    IPropertyHeader IProperty.Header => this;
}