namespace X2CharacterPool.Serialization;

public static class X2BinFileReadInitializer
{
    public static async ValueTask<X2BinReader> InitializeAsync(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        X2BinReader reader = new() { Stream = stream };
        await ReadValidateFileHeader(reader);

        return reader;
    }

    private static async ValueTask ReadValidateFileHeader(X2BinReader reader)
    {
        //Verify Magic Number
        
        const int magicNumber = -1;
        
        if (await reader.ReadInt() != magicNumber)
        {
            throw new X2SerializationException("Incorrect Header: Unexpected Magic Number!");
        }
    }
}