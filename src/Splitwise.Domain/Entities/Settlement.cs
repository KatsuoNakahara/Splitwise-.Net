namespace Splitwise.Domain.Entities;

/// <summary>A payment made by FromUser to ToUser to settle a debt inside a group.</summary>
public class Settlement
{
    public int Id { get; set; }

    public int GroupId { get; set; }

    public string FromUserId { get; set; } = string.Empty;

    public string ToUserId { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime CreatedAt { get; set; }

    public Group Group { get; set; } = null!;
}
