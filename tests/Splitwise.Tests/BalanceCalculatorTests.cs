using Splitwise.Application.Balances;
using Splitwise.Domain.Entities;

namespace Splitwise.Tests;

[TestFixture]
public class BalanceCalculatorTests
{
    [Test]
    public void CalculateNetBalances_CalculatesCorrectNetBalances()
    {
        var expense = new Expense
        {
            PaidByUserId = "u1",
            Amount = 30m,
            Splits = new List<ExpenseSplit>
            {
                new() { UserId = "u1", Amount = 10m },
                new() { UserId = "u2", Amount = 10m },
                new() { UserId = "u3", Amount = 10m }
            }
        };

        var settlements = new List<Settlement>
        {
            new() { FromUserId = "u2", ToUserId = "u1", Amount = 5m }
        };

        Dictionary<string, decimal> net = BalanceCalculator.CalculateNetBalances(new[] { expense }, settlements);

        // u1: paid 30, split 10 -> +20. Settled: u2 paid u1 5 -> u1 net owes 5 less or has 5 less debt to receive, so +15
        Assert.That(net["u1"], Is.EqualTo(15m));
        // u2: paid 0, split 10 -> -10. Settled: paid 5 -> -5
        Assert.That(net["u2"], Is.EqualTo(-5m));
        // u3: paid 0, split 10 -> -10
        Assert.That(net["u3"], Is.EqualTo(-10m));
    }

    [Test]
    public void SimplifyDebts_SingleDebtorAndCreditor_SimplifiesCorrectly()
    {
        var net = new Dictionary<string, decimal>
        {
            ["u1"] = 50m,
            ["u2"] = -50m
        };

        IReadOnlyList<Debt> debts = BalanceCalculator.SimplifyDebts(net);

        Assert.That(debts, Has.Count.EqualTo(1));
        Assert.That(debts[0].FromUserId, Is.EqualTo("u2"));
        Assert.That(debts[0].ToUserId, Is.EqualTo("u1"));
        Assert.That(debts[0].Amount, Is.EqualTo(50m));
    }

    [Test]
    public void SimplifyDebts_MultipleDebtorsAndCreditors_SimplifiesCorrectly()
    {
        var net = new Dictionary<string, decimal>
        {
            ["alice"] = 30m,
            ["bob"] = 20m,
            ["charlie"] = -25m,
            ["david"] = -25m
        };

        IReadOnlyList<Debt> debts = BalanceCalculator.SimplifyDebts(net);

        decimal totalDebt = debts.Sum(d => d.Amount);
        Assert.That(totalDebt, Is.EqualTo(50m));
        Assert.That(debts.All(d => d.Amount > 0), Is.True);
    }
}
