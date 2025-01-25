using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Namezr.Infrastructure.Data;

public class EncryptedJsonDocumentConverter : ValueConverter<JsonDocument, byte[]>
{
    private static readonly JsonSerializerOptions? Options = null;
    
    public EncryptedJsonDocumentConverter(IDataProtector dataProtector) : base(
        x => dataProtector.Protect(JsonSerializer.SerializeToUtf8Bytes(x, Options)),
        x => JsonSerializer.Deserialize<JsonDocument>(dataProtector.Unprotect(x), Options)!
    )
    {
    }
}