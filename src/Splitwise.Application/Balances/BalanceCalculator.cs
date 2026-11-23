using Splitwise.Domain.Entities;

namespace Splitwise.Application.Balances;

public sealed record Debt(string FromUserId, string ToUserId, decimal Amount);

public static class BalanceCalculator
{
    /// <summary>Net balance per user: positive = the user is owed money, negative = the user owes money.</summary>
    public static Dictionary<string, decimal> CalculateNetBalances(IEnumerable<Expense> expenses, IEnumerable<Settlement> settlements)
    {
        ArgumentNullException.ThrowIfNull(expenses);
        ArgumentNullException.ThrowIfNull(settlements);

        var net = new Dictionary<string, decimal>();

        foreach (Expense expense in expenses)
        {
            Add(net, expense.PaidByUserId, expense.Amount);

            foreach (ExpenseSplit split in expense.Splits)
            {
                Add(net, split.UserId, -split.Amount);
            }
        }

        foreach (Settlement settlement in settlements)
        {
            Add(net, settlement.FromUserId, settlement.Amount);
            Add(net, settlement.ToUserId, -settlement.Amount);
        }

        return net;
    }

    /// <summary>Turns net balances into a short list of "who pays whom" transfers.</summary>
    public static IReadOnlyList<Debt> SimplifyDebts(IReadOnlyDictionary<string, decimal> net)
    {
        ArgumentNullException.ThrowIfNull(net);

        List<Entry> creditors = net
            .Where(p => p.Value > 0)
            .OrderByDescending(p => p.Value)
            .ThenBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => new Entry(p.Key, p.Value))
            .ToList();

        List<Entry> debtors = net
            .Where(p => p.Value < 0)
            .OrderBy(p => p.Value)
            .ThenBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => new Entry(p.Key, -p.Value))
            .ToList();

        var result = new List<Debt>();
        int d = 0;
        int c = 0;

        while (d < debtors.Count && c < creditors.Count)
        {
            decimal amount = Math.Min(debtors[d].Amount, creditors[c].Amount);
            result.Add(new Debt(debtors[d].UserId, creditors[c].UserId, amount));

            debtors[d].Amount -= amount;
            creditors[c].Amount -= amount;

            if (debtors[d].Amount == 0)
            {
                d++;
            }

            if (creditors[c].Amount == 0)
            {
                c++;
            }
        }

        return result;
    }

    private static void Add(Dictionary<string, decimal> net, string userId, decimal amount)
    {
        net[userId] = net.GetValueOrDefault(userId) + amount;
    }

    private sealed class Entry
    {
        public Entry(string userId, decimal amount)
        {
            this.UserId = userId;
            this.Amount = amount;
        }

        public string UserId { get; }

        public decimal Amount { get; set; }
    }
}
