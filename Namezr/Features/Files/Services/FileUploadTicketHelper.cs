using System.Security.Claims;

namespace Namezr.Features.Files.Services;

public interface IFileUploadTicketHelper
{
    string CreateForCurrentUser(NewFileRestrictions restrictions);
    string CreateForCurrentUser(UploadedFileInfo fileInfo);
    NewFileRestrictions UnprotectRestrictionsForCurrentUser(string ticket);
    UploadedFileInfo UnprotectUploadedForCurrentUser(string ticket);
}

[AutoConstructor]
[RegisterSingleton]
public partial class FileUploadTicketHelper : IFileUploadTicketHelper
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IFileUploadTicketService _ticketService;

    public string CreateForCurrentUser(NewFileRestrictions restrictions)
    {
        return _ticketService.CreateTicket(restrictions with
        {
            UserId = GetUserId(),
        });
    }

    public NewFileRestrictions UnprotectRestrictionsForCurrentUser(string ticket)
    {
        NewFileRestrictions restrictions = _ticketService.UnprotectNewFileRestrictions(ticket);
        ValidateUserMatches(restrictions.UserId);

        return restrictions;
    }

    public string CreateForCurrentUser(UploadedFileInfo fileInfo)
    {
        return _ticketService.CreateTicket(fileInfo with
        {
            UserId = GetUserId(),
        });
    }

    public UploadedFileInfo UnprotectUploadedForCurrentUser(string ticket)
    {
        UploadedFileInfo fileInfo = _ticketService.UnprotectUploadedFileInfo(ticket);
        ValidateUserMatches(fileInfo.UserId);

        return fileInfo;
    }

    private void ValidateUserMatches(string? ticketUserId)
    {
        string? currentUserId = GetUserId();

        if (ticketUserId != currentUserId)
        {
            throw new TicketUnprotectionFailedException(
                "Ticket was created for another user"
            );
        }
    }

    private string? GetUserId()
    {
        return _httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}