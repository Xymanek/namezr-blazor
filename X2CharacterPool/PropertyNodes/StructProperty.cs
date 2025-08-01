using System.Collections.Immutable;

namespace X2CharacterPool.PropertyNodes;

public record StructPropertyHeader : IPropertyHeader
{
    public const string TypeName = "StructProperty";
    string IPropertyHeader.TypeName => TypeName;

    public required string Name { get; init; }
    public required string StructType { get; init; }
    public required int ContentSizeBytes { get; init; }
}

/// <remarks>
/// Not a dictionary because
/// 1) Technically a degenerate case where the struct has duplicate names is possible
/// 2) We want to preserve the order of entries as they appear in the struct
/// </remarks>
public record StructProperty : IProperty<ImmutableList<IProperty>>
{
    public required StructPropertyHeader Header { get; init; }
    IPropertyHeader IProperty.Header => Header;

    public required ImmutableList<IProperty> Value { get; init; }
    object IProperty.Value => Value;
}
