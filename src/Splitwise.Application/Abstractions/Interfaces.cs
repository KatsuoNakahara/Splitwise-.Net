using Microsoft.EntityFrameworkCore;
using Splitwise.Application.Dtos;
using Splitwise.Domain.Entities;

namespace Splitwise.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<Group> Groups { get; }

    DbSet<GroupMember> GroupMembers { get; }

    DbSet<Expense> Expenses { get; }

    DbSet<ExpenseSplit> ExpenseSplits { get; }

    DbSet<Settlement> Settlements { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IUserDirectory
{
    Task<string?> FindUserIdByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, string>> GetDisplayNamesAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default);
}

public interface ITokenGenerator
{
    string Generate(string userId, string email, string displayName);
}

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}

public interface IGroupService
{
    Task<GroupDetailsDto> CreateAsync(string userId, CreateGroupRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GroupSummaryDto>> GetUserGroupsAsync(string userId, CancellationToken cancellationToken = default);

    Task<GroupDetailsDto> GetAsync(string userId, int groupId, CancellationToken cancellationToken = default);

    Task<GroupDetailsDto> AddMemberAsync(string userId, int groupId, AddMemberRequest request, CancellationToken cancellationToken = default);
}

public interface IExpenseService
{
    Task<ExpenseDto> AddAsync(string userId, int groupId, CreateExpenseRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExpenseDto>> ListAsync(string userId, int groupId, CancellationToken cancellationToken = default);
}

public interface IBalanceService
{
    Task<BalanceReportDto> GetBalancesAsync(string userId, int groupId, CancellationToken cancellationToken = default);

    Task<SettlementDto> AddSettlementAsync(string userId, int groupId, CreateSettlementRequest request, CancellationToken cancellationToken = default);
}
