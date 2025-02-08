using System.ComponentModel.DataAnnotations;

namespace Namezr.Infrastructure.OAuth;

public class OAuthClientOptions
{
    [Required]
    public string ClientId { get; set; } = null!;

    [Required]
    public string ClientSecret { get; set; } = null!;
}