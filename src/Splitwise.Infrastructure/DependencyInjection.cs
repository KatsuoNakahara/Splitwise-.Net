using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Splitwise.Application.Abstractions;
using Splitwise.Infrastructure.Identity;
using Splitwise.Infrastructure.Persistence;
using Splitwise.Infrastructure.Services;

namespace Splitwise.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        _ = services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
        _ = services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        _ = services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<AppDbContext>();

        _ = services.AddScoped<IUserDirectory, UserDirectory>();
        _ = services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
