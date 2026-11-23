namespace Splitwise.Domain.Entities;

public class Expense
{
    public int Id { get; set; }

    public int GroupId { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string PaidByUserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public Group Group { get; set; } = null!;

    public ICollection<ExpenseSplit> Splits { get; set; } = new List<ExpenseSplit>();
}
