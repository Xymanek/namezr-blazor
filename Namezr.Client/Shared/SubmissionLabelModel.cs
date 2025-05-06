using Havit.Blazor.Components.Web.Bootstrap;

namespace Namezr.Client.Shared;

public class SubmissionLabelModel
{
    public Guid Id { get; set; }
    
    public string? Text { get; set; }
    
    /// <summary>
    /// Shown on hover
    /// </summary>
    public string? Description { get; set; }

    public string? Colour { get; set; }

    public BootstrapIcon? Icon { get; set; }
    
    public bool IsSubmitterVisible { get; set; }
}