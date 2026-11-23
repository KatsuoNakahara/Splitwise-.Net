using Microsoft.Extensions.DependencyInjection;
using Splitwise.Application.Abstractions;
using Splitwise.Application.Services;

namespace Splitwise.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        _ = services.AddScoped<IGroupService, GroupService>();
        _ = services.AddScoped<IExpenseService, ExpenseService>();
        _ = services.AddScoped<IBalanceService, BalanceService>();
        return services;
    }
}
