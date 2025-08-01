using System.Collections.Immutable;
using X2CharacterPool.PropertyNodes;
using X2CharacterPool.Serialization;

namespace X2CharacterPool.Domain;

public record CharacterPool
{
    public required ECharacterPoolSelectionMode SelectionMode { get; init; }
    public required string PoolFileName { get; init; }

    // public required CharacterPoolDataElement CharacterPoolSerializeHelper { get; init; }
    // public required int GenderHelper { get; init; }
    public required string ImportDirectoryName { get; init; }

    public required ImmutableList<CharacterPoolDataElement> NativeCharacters { get; init; }
    public required ImmutableList<ExtraDataEntry> ExtraDatas { get; init; }

    public static async Task<CharacterPool> Load(X2BinReader reader)
    {
        ECharacterPoolSelectionMode selectionMode = ECharacterPoolSelectionMode.None;
        string poolFileName = string.Empty;
        // CharacterPoolDataElement serializeHelper = default!;
        // int genderHelper = 0;
        string importDirectoryName = string.Empty;
        ImmutableList<ExtraDataEntry>? extraDatas = null;

        bool foundInitialNone = false;
        
        while (true)
        {
            IPropertyHeader header = await reader.ReadPropertyHeaderOrBasic();
            if (header is NoneProperty)
            {
                foundInitialNone = true;
                break;
            }

            switch (header)
            {
                // CharacterPool is very special.
                // It's array<XComGameState_Unit>, which does not actually write any data inside the array.
                // Furthermore, the array is recorded to have a length of more than 0, so we cannot just use normal parsing
                // as that would gobble up other top-level properties into the content-less array.
                case ArrayPropertyHeader { Name: "CharacterPool" }:
                    continue;

                case IntProperty { Name: "SelectionMode" } selectionModeProperty:
                    selectionMode = (ECharacterPoolSelectionMode)selectionModeProperty.Value;
                    break;

                case StringProperty { Name: "PoolFileName" } poolFileNameProperty:
                    poolFileName = poolFileNameProperty.Value;
                    break;

                case StructPropertyHeader { Name: "CharacterPoolSerializeHelper" } serializeHelperHeader:
                    // Skip the individual struct - the values will be read at the end of the file
                    await reader.SkipStructContents(serializeHelperHeader);
                    break;

                // case IntProperty { Name: "GenderHelper" } genderHelperProperty:
                //     genderHelper = genderHelperProperty.Value;
                //     break;

                case StringProperty { Name: "ImportDirectoryName" } importDirProperty:
                    importDirectoryName = importDirProperty.Value;
                    break;

                case ArrayPropertyHeader { Name: "ExtraDatas" } arrayHeader:
                    ArrayProperty arrayProperty = await reader.ReadArrayContents(arrayHeader);
                    extraDatas = arrayProperty.Value
                        .Select(ExtraDataEntry.Load)
                        .ToImmutableList();
                    break;
            }
        }

        if (!foundInitialNone)
        {
            throw new X2SerializationException("CharacterPool did contain a top level NoneProperty.");
        }

        ImmutableList<CharacterPoolDataElement> nativeCharacters = await ReadCharacterPoolElements(reader);

        return new CharacterPool
        {
            SelectionMode = selectionMode,
            PoolFileName = poolFileName,
            // CharacterPoolSerializeHelper = serializeHelper ??
            //                                throw new X2SerializationException("Missing CharacterPoolSerializeHelper"),
            // GenderHelper = genderHelper,
            ImportDirectoryName = importDirectoryName,
            ExtraDatas = extraDatas ?? throw new X2SerializationException("Missing ExtraDatas in CharacterPool"),
            NativeCharacters = nativeCharacters,
        };
    }

    private static async ValueTask<ImmutableList<CharacterPoolDataElement>> ReadCharacterPoolElements(
        X2BinReader reader
    )
    {
        int count = await reader.ReadInt();

        ImmutableList<ArrayEntry> arrayContents = await reader.ReadArrayContents(count);

        return arrayContents
            .Select(CharacterPoolDataElement.Load)
            .ToImmutableList();
    }
}