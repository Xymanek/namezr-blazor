using System.Collections.Immutable;

namespace X2CharacterPool.PropertyNodes;

public record ArrayPropertyHeader : IPropertyHeader
{
    public const string TypeName = "ArrayProperty";
    string IPropertyHeader.TypeName => TypeName;

    public required string Name { get; init; }

    public required int Size { get; init; }
}

/// <summary>
/// <para>
/// The binary representation of an array is a list of list of properties.
/// </para>
/// <para>
/// Important: a list of structs lacks the per-item struct header.
/// The only way to know when the item ends is a <see cref="NoneProperty"/>.
/// Same with the end of the array itself (so 2 <see cref="NoneProperty"/>s at the end).
/// </para> 
/// </summary>
public record ArrayProperty : IProperty<ImmutableList<ArrayEntry>>
{
    public required ArrayPropertyHeader Header { get; init; }
    IPropertyHeader IProperty.Header => Header;

    public static ImmutableList<ArrayEntry> DefaultValue { get; } = [];
    
    public required ImmutableList<ArrayEntry> Value { get; init; }
    object IProperty.Value => Value;
}

public record ArrayEntry
{
    public required ImmutableList<IProperty> Properties { get; init; }
}