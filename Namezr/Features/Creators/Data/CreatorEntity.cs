using System.ComponentModel.DataAnnotations;

namespace Namezr.Features.Creators.Data;

public class CreatorEntity
{
    public Guid Id { get; set; }

    [MaxLength(100)]
    public required string DisplayName { get; set; }
    
    // TODO: Banner & small image
}