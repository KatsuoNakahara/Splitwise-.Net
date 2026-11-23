namespace Splitwise.Application.Balances;

public static class SplitCalculator
{
    /// <summary>Splits an amount between users as evenly as possible, to the cent. The sum of shares always equals the amount.</summary>
    public static IReadOnlyList<(string UserId, decimal Amount)> Equal(decimal amount, IReadOnlyList<string> userIds)
    {
        ArgumentNullException.ThrowIfNull(userIds);

        if (userIds.Count == 0)
        {
            throw new ArgumentException("At least one participant is required.", nameof(userIds));
        }

        long cents = (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
        int count = userIds.Count;
        long baseShare = cents / count;
        long remainder = cents % count;

        var result = new List<(string UserId, decimal Amount)>(count);

        for (int i = 0; i < count; i++)
        {
            long share = baseShare + (i < remainder ? 1 : 0);
            result.Add((userIds[i], share / 100m));
        }

        return result;
    }
}
