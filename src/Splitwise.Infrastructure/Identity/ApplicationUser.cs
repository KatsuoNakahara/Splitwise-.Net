using Microsoft.AspNetCore.Identity;

namespace Splitwise.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}
