using Microsoft.AspNetCore.Identity;
using Namezr.Features.Identity.Data;

namespace Namezr.Infrastructure.Auth;

public readonly struct ExternalSignInInfo
{
    public required ApplicationUser User { get; init; }
    
    public required ExternalLoginInfo ExternalLoginInfo { get; init; }
}