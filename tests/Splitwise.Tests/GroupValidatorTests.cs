using Splitwise.Application.Services;

namespace Splitwise.Tests;

[TestFixture]
public class GroupValidatorTests
{
    [Test]
    public void ValidateCreate_ValidInput_HasNoErrors()
    {
        var errors = GroupValidator.ValidateCreate("Trip", new[] { "user1@example.com", "user2@example.com" });
        Assert.That(errors, Is.Empty);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void ValidateCreate_EmptyName_ReturnsError(string? name)
    {
        var errors = GroupValidator.ValidateCreate(name, null);
        Assert.That(errors, Does.Contain("Group name is required."));
    }

    [Test]
    public void ValidateCreate_TooLongName_ReturnsError()
    {
        string longName = new('a', GroupValidator.MaxNameLength + 1);
        var errors = GroupValidator.ValidateCreate(longName, null);
        Assert.That(errors, Does.Contain($"Group name must not exceed {GroupValidator.MaxNameLength} characters."));
    }

    [Test]
    public void ValidateCreate_DuplicateEmails_ReturnsError()
    {
        var errors = GroupValidator.ValidateCreate("Roommates", new[] { "bob@example.com", "BOB@example.com" });
        Assert.That(errors, Does.Contain("Member emails must be unique."));
    }

    [Test]
    public void ValidateCreate_TooManyMembers_ReturnsError()
    {
        var emails = Enumerable.Range(1, GroupValidator.MaxMembers).Select(i => $"user{i}@example.com").ToList();
        var errors = GroupValidator.ValidateCreate("BigGroup", emails);
        Assert.That(errors, Does.Contain($"A group cannot have more than {GroupValidator.MaxMembers} members."));
    }
}
