using Microsoft.AspNetCore.Identity;
using Namezr.Features.Identity.Data;

namespace Namezr.Infrastructure.Auth;

public readonly struct ExternalSignInInfo
{
    public required ApplicationUser User { get; init; }
    public required bool IsPersistent { get; init; }
    
    public required string LoginProvider { get; init; }
    public required ExternalLoginInfo ExternalLoginInfo { get; init; }
}