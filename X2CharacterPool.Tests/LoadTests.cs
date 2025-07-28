using System.Reflection;
using Shouldly;
using X2CharacterPool.Domain;
using X2CharacterPool.Serialization;

namespace X2CharacterPool.Tests;

public class LoadTests
{
    [Fact]
    public async Task Deadpool()
    {
        string baseDir = Assembly.GetExecutingAssembly().Location;
        string filePath = Path.Combine(
            Path.GetDirectoryName(baseDir) ?? throw new InvalidOperationException("Could not determine base directory"),
            "TestFiles",
            "AM bins",
            "Dacnis Joyeuse - Deadput.bin"
        );

        await using FileStream fileStream = File.OpenRead(filePath);
        X2BinReader reader = await X2BinFileReadInitializer.InitializeAsync(fileStream);

        CharacterPool characterPool = await CharacterPool.Load(reader);
        
        characterPool.NativeCharacters.Count.ShouldBe(1);
        characterPool.NativeCharacters[0].FirstName.ShouldBe("Dacnis");
        characterPool.NativeCharacters[0].LastName.ShouldBe("Joyeuse");
        
        characterPool.ExtraDatas.Count.ShouldBe(1);
        characterPool.ExtraDatas[0].AppearanceStore.Count.ShouldBe(7);
        
        characterPool.ExtraDatas[0].CharPoolData.ShouldBe(characterPool.NativeCharacters[0]);
    }
    
    [Fact]
    public async Task Season8()
    {
        string baseDir = Assembly.GetExecutingAssembly().Location;
        string filePath = Path.Combine(
            Path.GetDirectoryName(baseDir) ?? throw new InvalidOperationException("Could not determine base directory"),
            "TestFiles",
            "AM bins",
            "Complete Season 8+9 Pool.bin"
        );

        await using FileStream fileStream = File.OpenRead(filePath);
        X2BinReader reader = await X2BinFileReadInitializer.InitializeAsync(fileStream);

        CharacterPool characterPool = await CharacterPool.Load(reader);
        
        characterPool.NativeCharacters.Count.ShouldBe(202);
        characterPool.ExtraDatas.Count.ShouldBe(202);

        // Ensure that all match
        HashSet<CharacterPoolDataElement> nativeChars = characterPool.NativeCharacters.ToHashSet();
        nativeChars.ExceptWith(characterPool.ExtraDatas.Select(entry => entry.CharPoolData));
        nativeChars.Count.ShouldBe(0);
    }
}