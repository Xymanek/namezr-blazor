namespace Namezr.Features.Files.Services;

public class TicketUnprotectionFailedException : Exception
{
    public TicketUnprotectionFailedException()
    {
    }

    public TicketUnprotectionFailedException(string? message) : base(message)
    {
    }

    public TicketUnprotectionFailedException(
        string? message, Exception? innerException
    ) : base(message, innerException)
    {
    }
}