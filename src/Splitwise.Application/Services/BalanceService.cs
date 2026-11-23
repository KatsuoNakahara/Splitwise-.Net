using Microsoft.EntityFrameworkCore;
using Splitwise.Application.Abstractions;
using Splitwise.Application.Balances;
using Splitwise.Application.Dtos;
using Splitwise.Application.Exceptions;
using Splitwise.Domain.Entities;

namespace Splitwise.Application.Services;

public sealed class BalanceService : IBalanceService
{
    private readonly IAppDbContext db;
    private readonly IUserDirectory users;

    public BalanceService(IAppDbContext db, IUserDirectory users)
    {
        this.db = db;
        this.users = users;
    }

    public async Task<BalanceReportDto> GetBalancesAsync(string userId, int groupId, CancellationToken cancellationToken = default)
    {
        Group group = await GroupAccess.GetForMemberAsync(this.db, userId, groupId, cancellationToken);

        List<Expense> expenses = await this.db.Expenses
            .Include(e => e.Splits)
            .Where(e => e.GroupId == groupId)
            .ToListAsync(cancellationToken);

        List<Settlement> settlements = await this.db.Settlements
            .Where(s => s.GroupId == groupId)
            .ToListAsync(cancellationToken);

        Dictionary<string, decimal> net = BalanceCalculator.CalculateNetBalances(expenses, settlements);

        foreach (GroupMember member in group.Members)
        {
            if (!net.ContainsKey(member.UserId))
            {
                net[member.UserId] = 0m;
            }
        }

        IReadOnlyList<Debt> debts = BalanceCalculator.SimplifyDebts(net);
        IReadOnlyDictionary<string, string> names = await this.users.GetDisplayNamesAsync(net.Keys, cancellationToken);

        List<UserBalanceDto> balances = net
            .Select(p => new UserBalanceDto(p.Key, names.GetValueOrDefault(p.Key, p.Key), p.Value))
            .OrderBy(b => b.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        List<DebtDto> debtDtos = debts
            .Select(d => new DebtDto(
                d.FromUserId,
                names.GetValueOrDefault(d.FromUserId, d.FromUserId),
                d.ToUserId,
                names.GetValueOrDefault(d.ToUserId, d.ToUserId),
                d.Amount))
            .ToList();

        return new BalanceReportDto(balances, debtDtos);
    }

    public async Task<SettlementDto> AddSettlementAsync(string userId, int groupId, CreateSettlementRequest request, CancellationToken cancellationToken = default)
    {
        Group group = await GroupAccess.GetForMemberAsync(this.db, userId, groupId, cancellationToken);
        var errors = new List<string>();

        if (!group.Members.Any(m => m.UserId == request.ToUserId))
        {
            errors.Add("The recipient must be a member of the group.");
        }

        if (request.ToUserId == userId)
        {
            errors.Add("You cannot settle up with yourself.");
        }

        if (request.Amount <= 0)
        {
            errors.Add("Amount must be greater than zero.");
        }
        else if (!GroupAccess.IsValidMoney(request.Amount))
        {
            errors.Add("Amount must have at most 2 decimal places.");
        }

        if (errors.Count > 0)
        {
            throw new ValidationFailedException(errors);
        }

        var settlement = new Settlement
        {
            GroupId = groupId,
            FromUserId = userId,
            ToUserId = request.ToUserId,
            Amount = request.Amount,
            CreatedAt = DateTime.UtcNow,
        };

        _ = this.db.Settlements.Add(settlement);
        _ = await this.db.SaveChangesAsync(cancellationToken);

        return new SettlementDto(settlement.Id, groupId, settlement.FromUserId, settlement.ToUserId, settlement.Amount, settlement.CreatedAt);
    }
}
