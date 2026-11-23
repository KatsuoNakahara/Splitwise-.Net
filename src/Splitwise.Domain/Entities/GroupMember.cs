namespace Splitwise.Domain.Entities;

public class GroupMember
{
    public int GroupId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public Group Group { get; set; } = null!;
}
