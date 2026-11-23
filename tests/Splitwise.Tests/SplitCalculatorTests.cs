using Splitwise.Application.Balances;

namespace Splitwise.Tests;

[TestFixture]
public class SplitCalculatorTests
{
    [Test]
    public void Equal_SplitsEvenlyWithoutRemainder()
    {
        var users = new[] { "u1", "u2", "u3" };
        var splits = SplitCalculator.Equal(30m, users);

        Assert.That(splits, Has.Count.EqualTo(3));
        Assert.That(splits.All(s => s.Amount == 10m), Is.True);
        Assert.That(splits.Sum(s => s.Amount), Is.EqualTo(30m));
    }

    [Test]
    public void Equal_HandlesCentRemainderEquitably()
    {
        var users = new[] { "u1", "u2", "u3" };
        var splits = SplitCalculator.Equal(100m, users);

        Assert.That(splits, Has.Count.EqualTo(3));
        Assert.That(splits.Sum(s => s.Amount), Is.EqualTo(100m));
        Assert.That(splits[0].Amount, Is.EqualTo(33.34m));
        Assert.That(splits[1].Amount, Is.EqualTo(33.33m));
        Assert.That(splits[2].Amount, Is.EqualTo(33.33m));
    }

    [Test]
    public void Equal_ThrowsWhenNoUsersProvided()
    {
        Assert.Throws<ArgumentException>(() => SplitCalculator.Equal(50m, Array.Empty<string>()));
    }
}
