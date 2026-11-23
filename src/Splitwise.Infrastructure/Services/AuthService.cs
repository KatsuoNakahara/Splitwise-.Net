using Microsoft.AspNetCore.Identity;
using Splitwise.Application.Abstractions;
using Splitwise.Application.Dtos;
using Splitwise.Application.Exceptions;
using Splitwise.Infrastructure.Identity;

namespace Splitwise.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private const int MaxDisplayNameLength = 100;

    private readonly UserManager<ApplicationUser> userManager;
    private readonly ITokenGenerator tokenGenerator;

    public AuthService(UserManager<ApplicationUser> userManager, ITokenGenerator tokenGenerator)
    {
        this.userManager = userManager;
        this.tokenGenerator = tokenGenerator;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@', StringComparison.Ordinal))
        {
            errors.Add("A valid email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            errors.Add("Display name is required.");
        }
        else if (request.DisplayName.Trim().Length > MaxDisplayNameLength)
        {
            errors.Add($"Display name must not exceed {MaxDisplayNameLength} characters.");
        }

        if (errors.Count > 0)
        {
            throw new ValidationFailedException(errors);
        }

        var user = new ApplicationUser
        {
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            DisplayName = request.DisplayName.Trim(),
        };

        IdentityResult result = await this.userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            throw new ValidationFailedException(result.Errors.Select(e => e.Description).ToList());
        }

        return this.ToResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        ApplicationUser? user = await this.userManager.FindByEmailAsync(request.Email.Trim());

        if (user is null || !await this.userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        return this.ToResponse(user);
    }

    private AuthResponse ToResponse(ApplicationUser user)
    {
        string email = user.Email ?? string.Empty;
        string token = this.tokenGenerator.Generate(user.Id, email, user.DisplayName);
        return new AuthResponse(token, user.Id, email, user.DisplayName);
    }
}
