using System.Security.Claims;
using Splitwise.Application.Exceptions;

namespace Splitwise.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedException("User identity is missing.");
    }
}
