namespace Splitwise.Domain.Entities;

public class ExpenseSplit
{
    public int Id { get; set; }

    public int ExpenseId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public Expense Expense { get; set; } = null!;
}
