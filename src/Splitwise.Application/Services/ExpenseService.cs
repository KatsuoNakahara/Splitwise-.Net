using Microsoft.EntityFrameworkCore;
using Splitwise.Application.Abstractions;
using Splitwise.Application.Balances;
using Splitwise.Application.Dtos;
using Splitwise.Application.Exceptions;
using Splitwise.Domain.Entities;

namespace Splitwise.Application.Services;

public sealed class ExpenseService : IExpenseService
{
    private const int MaxDescriptionLength = 200;
    private const decimal MaxAmount = 1_000_000_000m;

    private readonly IAppDbContext db;
    private readonly IUserDirectory users;

    public ExpenseService(IAppDbContext db, IUserDirectory users)
    {
        this.db = db;
        this.users = users;
    }

    public async Task<ExpenseDto> AddAsync(string userId, int groupId, CreateExpenseRequest request, CancellationToken cancellationToken = default)
    {
        Group group = await GroupAccess.GetForMemberAsync(this.db, userId, groupId, cancellationToken);
        HashSet<string> memberIds = group.Members.Select(m => m.UserId).ToHashSet();

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            errors.Add("Description is required.");
        }
        else if (request.Description.Trim().Length > MaxDescriptionLength)
        {
            errors.Add($"Description must not exceed {MaxDescriptionLength} characters.");
        }

        if (request.Amount <= 0)
        {
            errors.Add("Amount must be greater than zero.");
        }
        else if (!GroupAccess.IsValidMoney(request.Amount))
        {
            errors.Add("Amount must have at most 2 decimal places.");
        }
        else if (request.Amount > MaxAmount)
        {
            errors.Add($"Amount must not exceed {MaxAmount}.");
        }

        if (!memberIds.Contains(request.PaidByUserId))
        {
            errors.Add("The payer must be a member of the group.");
        }

        if (errors.Count > 0)
        {
            throw new ValidationFailedException(errors);
        }

        IReadOnlyList<(string UserId, decimal Amount)> splits = BuildSplits(request, memberIds, errors);

        if (errors.Count > 0)
        {
            throw new ValidationFailedException(errors);
        }

        var expense = new Expense
        {
            GroupId = groupId,
            Description = request.Description.Trim(),
            Amount = request.Amount,
            PaidByUserId = request.PaidByUserId,
            CreatedAt = DateTime.UtcNow,
        };

        foreach ((string splitUserId, decimal splitAmount) in splits)
        {
            expense.Splits.Add(new ExpenseSplit { UserId = splitUserId, Amount = splitAmount });
        }

        _ = this.db.Expenses.Add(expense);
        _ = await this.db.SaveChangesAsync(cancellationToken);

        IReadOnlyDictionary<string, string> names = await this.users.GetDisplayNamesAsync(memberIds, cancellationToken);
        return ToDto(expense, names);
    }

    public async Task<IReadOnlyList<ExpenseDto>> ListAsync(string userId, int groupId, CancellationToken cancellationToken = default)
    {
        Group group = await GroupAccess.GetForMemberAsync(this.db, userId, groupId, cancellationToken);

        List<Expense> expenses = await this.db.Expenses
            .Include(e => e.Splits)
            .Where(e => e.GroupId == groupId)
            .OrderBy(e => e.Id)
            .ToListAsync(cancellationToken);

        IEnumerable<string> allUserIds = group.Members.Select(m => m.UserId)
            .Concat(expenses.Select(e => e.PaidByUserId))
            .Concat(expenses.SelectMany(e => e.Splits.Select(s => s.UserId)));

        IReadOnlyDictionary<string, string> names = await this.users.GetDisplayNamesAsync(allUserIds.Distinct(), cancellationToken);
        return expenses.Select(e => ToDto(e, names)).ToList();
    }

    private static IReadOnlyList<(string UserId, decimal Amount)> BuildSplits(
        CreateExpenseRequest request,
        HashSet<string> memberIds,
        List<string> errors)
    {
        switch (request.SplitType)
        {
            case SplitType.Equal:
            {
                IReadOnlyList<string> participants = request.ParticipantUserIds is { Count: > 0 }
                    ? request.ParticipantUserIds
                    : memberIds.ToList();

                if (participants.Distinct().Count() != participants.Count)
                {
                    errors.Add("Participants must be unique.");
                }

                if (participants.Any(p => !memberIds.Contains(p)))
                {
                    errors.Add("All participants must be members of the group.");
                }

                return errors.Count > 0
                    ? new List<(string UserId, decimal Amount)>()
                    : SplitCalculator.Equal(request.Amount, participants);
            }

            case SplitType.Exact:
            {
                IReadOnlyList<ExactSplitInput>? exact = request.ExactSplits;

                if (exact is null || exact.Count == 0)
                {
                    errors.Add("Exact splits are required for the Exact split type.");
                    return new List<(string UserId, decimal Amount)>();
                }

                if (exact.Select(s => s.UserId).Distinct().Count() != exact.Count)
                {
                    errors.Add("Each user can appear only once in the splits.");
                }

                if (exact.Any(s => !memberIds.Contains(s.UserId)))
                {
                    errors.Add("All users in the splits must be members of the group.");
                }

                if (exact.Any(s => s.Amount < 0 || !GroupAccess.IsValidMoney(s.Amount)))
                {
                    errors.Add("Split amounts must be non-negative with at most 2 decimal places.");
                }

                if (errors.Count == 0 && exact.Sum(s => s.Amount) != request.Amount)
                {
                    errors.Add("The sum of the splits must equal the expense amount.");
                }

                return errors.Count > 0
                    ? new List<(string UserId, decimal Amount)>()
                    : exact.Select(s => (s.UserId, s.Amount)).ToList();
            }

            default:
                errors.Add("Unknown split type.");
                return new List<(string UserId, decimal Amount)>();
        }
    }

    private static ExpenseDto ToDto(Expense expense, IReadOnlyDictionary<string, string> names)
    {
        List<ExpenseSplitDto> splits = expense.Splits
            .Select(s => new ExpenseSplitDto(s.UserId, names.GetValueOrDefault(s.UserId, s.UserId), s.Amount))
            .ToList();

        return new ExpenseDto(
            expense.Id,
            expense.GroupId,
            expense.Description,
            expense.Amount,
            expense.PaidByUserId,
            names.GetValueOrDefault(expense.PaidByUserId, expense.PaidByUserId),
            expense.CreatedAt,
            splits);
    }
}
