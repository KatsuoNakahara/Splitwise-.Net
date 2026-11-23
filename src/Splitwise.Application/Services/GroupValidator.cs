namespace Splitwise.Application.Services;

public static class GroupValidator
{
    public const int MaxNameLength = 100;
    public const int MaxMembers = 50;

    public static IReadOnlyList<string> ValidateCreate(string? name, IReadOnlyCollection<string>? memberEmails)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Group name is required.");
        }
        else if (name.Trim().Length > MaxNameLength)
        {
            errors.Add($"Group name must not exceed {MaxNameLength} characters.");
        }

        if (memberEmails is not null)
        {
            if (memberEmails.Any(e => string.IsNullOrWhiteSpace(e)))
            {
                errors.Add("Member emails must not be empty.");
            }

            List<string> cleaned = memberEmails
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim())
                .ToList();

            if (cleaned.Count != cleaned.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            {
                errors.Add("Member emails must be unique.");
            }

            if (cleaned.Count + 1 > MaxMembers)
            {
                errors.Add($"A group cannot have more than {MaxMembers} members.");
            }
        }

        return errors;
    }
}
