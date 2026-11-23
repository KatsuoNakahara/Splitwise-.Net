using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Splitwise.Application.Abstractions;

namespace Splitwise.Api.Auth;

public sealed class JwtTokenGenerator : ITokenGenerator
{
    private readonly JwtOptions options;

    public JwtTokenGenerator(JwtOptions options)
    {
        this.options = options;
    }

    public string Generate(string userId, string email, string displayName)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this.options.Key));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, displayName),
            }),
            Expires = DateTime.UtcNow.AddMinutes(this.options.ExpiresMinutes),
            Issuer = this.options.Issuer,
            Audience = this.options.Audience,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
