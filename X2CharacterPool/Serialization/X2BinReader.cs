using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using CommunityToolkit.Diagnostics;
using X2CharacterPool.PropertyNodes;

namespace X2CharacterPool.Serialization;

// Currently the logic relies on getting 0 bytes if the stream has ended
// and I have no time/reason to figure out the correct approach.
[SuppressMessage("Reliability", "CA2022:Avoid inexact read with \'Stream.Read\'")]
[SuppressMessage("ReSharper", "StreamReadReturnValueIgnored")]
[SuppressMessage("ReSharper", "MustUseReturnValue")]
public class X2BinReader
{
    public required Stream Stream { get; init; }

    #region BasicReads

    public async ValueTask<byte[]> ReadBytes(int amountOfBytes)
    {
        // Sanity check against reading too much
        Guard.IsLessThanOrEqualTo(amountOfBytes, Stream.Length);

        byte[] buffer = new byte[amountOfBytes];
        await Stream.ReadAsync(buffer, 0, buffer.Length);

        return buffer;
    }

    public async ValueTask<int> ReadInt()
    {
        byte[] buffer = new byte[4];
        await Stream.ReadAsync(buffer, 0, buffer.Length);

        return BitConverter.ToInt32(buffer);
    }

    public async ValueTask<bool> ReadBool()
    {
        byte[] buffer = new byte[1];
        await Stream.ReadAsync(buffer, 0, buffer.Length);

        return BitConverter.ToBoolean(buffer);
    }

    private const int MaxPermittedStringLength = 5_000;

    private static readonly Encoding AnsiEncoding = Encoding.GetEncoding("ISO-8859-1");
    private static readonly Encoding UtfEncoding = Encoding.Unicode;

    public async ValueTask<string> ReadString()
    {
        int length = await ReadInt();

        switch (length)
        {
            case 0:
                return string.Empty;

            case > 0:
            {
                // Sanity check against allocating too much memory
                Guard.IsLessThanOrEqualTo(length, MaxPermittedStringLength);

                byte[] buffer = new byte[length];
                await Stream.ReadAsync(buffer, 0, buffer.Length);

                return AnsiEncoding.GetString(buffer, 0, length - 1);
            }

            case < 0:
            {
                // Sanity check against allocating too much memory
                Guard.IsGreaterThanOrEqualTo(length, -MaxPermittedStringLength);

                // Negative means unicode encoding - make the value positive and account for 2 bytes per character
                length *= -2;

                byte[] buffer = new byte[length];
                await Stream.ReadAsync(buffer, 0, buffer.Length);

                return UtfEncoding.GetString(buffer, 0, length - 2);
            }
        }
    }

    public async ValueTask ReadPadding()
    {
        if (await ReadInt() != 0)
        {
            throw new X2SerializationException("Padding Error: Did not Read Expected Integer 0");
        }
    }

    #endregion

    #region PropertyCreation

    /// <summary>
    /// Reads the property present at the current stream offset,
    /// but avoids reading the array/struct contents 
    /// </summary>
    public async ValueTask<IPropertyHeader> ReadPropertyHeaderOrBasic()
    {
        string name = await ReadString();
        await ReadPadding();

        // None property - the name acts like both type and name
        if (name == NoneProperty.TypeName)
        {
            return new NoneProperty();
        }

        string type = await ReadString();
        await ReadPadding();

        int size = await ReadInt();
        await ReadPadding();

        switch (type)
        {
            case ArrayPropertyHeader.TypeName:
                int originalSize = await ReadInt();
                return new ArrayPropertyHeader
                {
                    Name = name,
                    Size = originalSize,
                };

            case IntProperty.TypeName:
                return new IntProperty
                {
                    Name = name,
                    Value = await ReadInt(),
                };

            case BoolProperty.TypeName:
                return new BoolProperty
                {
                    Name = name,
                    Value = await ReadBool(),
                };

            case NameProperty.TypeName:
                string nameValue = await ReadString();
                await ReadInt(); // name properties have an extra int after for some reason
                return new NameProperty
                {
                    Name = name,
                    Value = nameValue,
                };

            case StringProperty.TypeName:
                return new StringProperty
                {
                    Name = name,
                    Value = await ReadString(),
                };

            case ByteProperty.TypeName:
                string enumType = await ReadString();
                await ReadPadding();

                string enumValue = await ReadString();
                await ReadInt(); // name properties have an extra int after for some reason

                return new ByteProperty
                {
                    Name = name,
                    EnumName = enumType,
                    Value = enumValue,
                };

            case StructPropertyHeader.TypeName:
                // A struct property has an extra string and integer between the size and the data
                string structType = await ReadString();
                await ReadInt();

                return new StructPropertyHeader
                {
                    Name = name,
                    StructType = structType,
                    ContentSizeBytes = size,
                };

            default:
                throw new X2SerializationException(
                    $"Property Creation Error: Cannot make property named {name} and of type {type}."
                );
        }
    }

    public async ValueTask<IProperty> ReadPropertyFull()
    {
        IPropertyHeader header = await ReadPropertyHeaderOrBasic();

        // The basic case
        if (header is IProperty property) return property;

        // The complex case - array or struct
        return header switch
        {
            ArrayPropertyHeader arrayHeader => await ReadArrayContents(arrayHeader),
            StructPropertyHeader structHeader => await ReadStructContents(structHeader),

            _ => throw new UnreachableException(),
        };
    }

    public async ValueTask SkipStructContents(StructPropertyHeader header)
    {
        Stream.Seek(header.ContentSizeBytes, SeekOrigin.Current);
    }

    public async ValueTask<StructProperty> ReadStructContents(StructPropertyHeader header)
    {
        // Load the entire struct into memory - this ensures that we do not overrun it
        byte[] structContentsBuffer = await ReadBytes(header.ContentSizeBytes);
        using MemoryStream structContentsStream = new(structContentsBuffer);
        X2BinReader structContentReader = new() { Stream = structContentsStream };

        ImmutableList<IProperty>.Builder builder = ImmutableList<IProperty>.Empty.ToBuilder();

        while (true)
        {
            if (structContentsStream.Position >= structContentsStream.Length)
            {
                // If we reached the end of the stream, we assume the struct is done
                break;
            }

            IProperty property = await structContentReader.ReadPropertyFull();

            // Is end of struct?
            if (property is NoneProperty) break;

            // Add the property to the struct
            builder.Add(property);
        }

        return new StructProperty
        {
            Header = header,
            Value = builder.ToImmutable(),
        };
    }

    public async ValueTask<ArrayProperty> ReadArrayContents(ArrayPropertyHeader header)
    {
        ImmutableList<ArrayEntry> value = await ReadArrayContents(header.Size);

        return new ArrayProperty
        {
            Header = header,
            Value = value,
        };
    }

    public async ValueTask<ImmutableList<ArrayEntry>> ReadArrayContents(int size)
    {
        ImmutableList<ArrayEntry>.Builder entries = ImmutableList<ArrayEntry>.Empty.ToBuilder();
        for (int i = 0; i < size; i++)
        {
            ArrayEntry entry = await ReadArrayEntry();

            // If we got nothing, then the array has ended.
            // (weird that it does not match the size, but I guess it's safe to make this assumption)
            if (entry.Properties.IsEmpty) break;

            entries.Add(entry);
        }

        ImmutableList<ArrayEntry> value = entries.ToImmutable();
        return value;
    }

    public async ValueTask<ArrayEntry> ReadArrayEntry()
    {
        ImmutableList<IProperty>.Builder properties = ImmutableList<IProperty>.Empty.ToBuilder();

        while (true)
        {
            IProperty property = await ReadPropertyFull();
            if (property is NoneProperty) break;

            properties.Add(property);
        }

        return new ArrayEntry
        {
            Properties = properties.ToImmutable(),
        };
    }

    #endregion
}