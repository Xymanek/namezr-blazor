using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;

namespace Namezr.Features.Files.Services;

public interface IFileUploadTicketService
{
    string CreateTicket(NewFileRestrictions restrictions);
    string CreateTicket(UploadedFileInfo fileInfo);

    /// <exception cref="TicketUnprotectionFailedException"></exception>
    NewFileRestrictions UnprotectNewFileRestrictions(string ticket);

    /// <exception cref="TicketUnprotectionFailedException"></exception>
    UploadedFileInfo UnprotectUploadedFileInfo(string ticket);
}

[RegisterSingleton]
public class FileUploadTicketService(
    IDataProtectionProvider dataProtectionProvider
) : IFileUploadTicketService
{
    #region NewFile

    private const string NewFileProtectorName = "Namezr.Ticket.NewFileRestrictions";

    private readonly ITimeLimitedDataProtector _newFileDataProtector = dataProtectionProvider
        .CreateProtector(NewFileProtectorName)
        .ToTimeLimitedDataProtector();

    private static readonly TimeSpan DefaultNewFileLifetime = TimeSpan.FromHours(3);

    public string CreateTicket(NewFileRestrictions restrictions)
    {
        return ProtectTicket(restrictions, _newFileDataProtector, DefaultNewFileLifetime);
    }

    public NewFileRestrictions UnprotectNewFileRestrictions(string ticket)
    {
        return UnprotectTicket<NewFileRestrictions>(ticket, _newFileDataProtector);
    }

    #endregion

    #region NewFile

    private const string UploadedFileProtectorName = "Namezr.Ticket.UploadedFile";

    private readonly ITimeLimitedDataProtector _uploadedFileDataProtector = dataProtectionProvider
        .CreateProtector(UploadedFileProtectorName)
        .ToTimeLimitedDataProtector();

    private static readonly TimeSpan DefaultUploadedFileLifetime = TimeSpan.FromHours(3);

    public string CreateTicket(UploadedFileInfo fileInfo)
    {
        return ProtectTicket(fileInfo, _uploadedFileDataProtector, DefaultUploadedFileLifetime);
    }

    public UploadedFileInfo UnprotectUploadedFileInfo(string ticket)
    {
        return UnprotectTicket<UploadedFileInfo>(ticket, _uploadedFileDataProtector);
    }

    #endregion

    // TODO: replace STJ with msgpack or protobuf

    private static string ProtectTicket<T>(T content, ITimeLimitedDataProtector protector, TimeSpan lifetime)
    {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(content);
        byte[] protectedBytes = protector.Protect(bytes, lifetime);

        return Base64UrlTextEncoder.Encode(protectedBytes);
    }

    private static T UnprotectTicket<T>(string ticket, ITimeLimitedDataProtector protector)
    {
        byte[] protectedData;

        try
        {
            protectedData = Base64UrlTextEncoder.Decode(ticket);
        }
        catch (FormatException e)
        {
            throw new TicketUnprotectionFailedException("Failed to base64 decode the ticket", e);
        }

        byte[] unprotectedBytes;
        try
        {
            unprotectedBytes = protector.Unprotect(protectedData);
        }
        catch (CryptographicException e)
        {
            throw new TicketUnprotectionFailedException("Failed to unprotect the ticket", e);
        }

        try
        {
            return JsonSerializer.Deserialize<T>(unprotectedBytes) ??
                   throw new TicketUnprotectionFailedException("Deserialized the ticket as null");
        }
        catch (JsonException e)
        {
            throw new TicketUnprotectionFailedException("Ticket value is malformed", e);
        }
    }
}