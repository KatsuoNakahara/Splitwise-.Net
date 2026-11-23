using Microsoft.EntityFrameworkCore;
using Splitwise.Application.Abstractions;
using Splitwise.Application.Dtos;
using Splitwise.Application.Exceptions;
using Splitwise.Application.Services;
using Splitwise.Domain.Entities;
using Splitwise.Infrastructure.Persistence;

namespace Splitwise.Tests;

public class FakeUserDirectory : IUserDirectory
{
    private readonly Dictionary<string, string> usersByEmail = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> displayNamesById = new();

    public void AddUser(string id, string email, string displayName)
    {
        usersByEmail[email] = id;
        displayNamesById[id] = displayName;
    }

    public Task<string?> FindUserIdByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        usersByEmail.TryGetValue(email, out string? id);
        return Task.FromResult(id);
    }

    public Task<IReadOnlyDictionary<string, string>> GetDisplayNamesAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<string, string> result = userIds
            .Distinct()
            .ToDictionary(id => id, id => displayNamesById.GetValueOrDefault(id, id));
        return Task.FromResult(result);
    }
}

[TestFixture]
public class ServiceTests
{
    private AppDbContext db = null!;
    private FakeUserDirectory userDirectory = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        db = new AppDbContext(options);
        userDirectory = new FakeUserDirectory();
    }

    [TearDown]
    public void TearDown()
    {
        db.Dispose();
    }

    [Test]
    public async Task GroupService_CreateAndGet_WorksAsExpected()
    {
        userDirectory.AddUser("u1", "alice@example.com", "Alice");
        userDirectory.AddUser("u2", "bob@example.com", "Bob");

        var groupService = new GroupService(db, userDirectory);
        var created = await groupService.CreateAsync("u1", new CreateGroupRequest("Vacation", new[] { "bob@example.com" }));

        Assert.That(created.Name, Is.EqualTo("Vacation"));
        Assert.That(created.Members, Has.Count.EqualTo(2));

        var fetched = await groupService.GetAsync("u1", created.Id);
        Assert.That(fetched.Id, Is.EqualTo(created.Id));
    }

    [Test]
    public void GroupService_GetByNonMember_ThrowsForbiddenException()
    {
        var groupService = new GroupService(db, userDirectory);
        var group = new Group { Name = "Secret", CreatedByUserId = "u1" };
        group.Members.Add(new GroupMember { UserId = "u1" });
        db.Groups.Add(group);
        db.SaveChanges();

        Assert.ThrowsAsync<ForbiddenException>(async () => await groupService.GetAsync("stranger", group.Id));
    }

    [Test]
    public async Task ExpenseService_AddEqualExpense_CreatesSplits()
    {
        userDirectory.AddUser("u1", "alice@test.com", "Alice");
        userDirectory.AddUser("u2", "bob@test.com", "Bob");

        var group = new Group { Name = "Flat", CreatedByUserId = "u1" };
        group.Members.Add(new GroupMember { UserId = "u1" });
        group.Members.Add(new GroupMember { UserId = "u2" });
        db.Groups.Add(group);
        await db.SaveChangesAsync();

        var expenseService = new ExpenseService(db, userDirectory);
        var expense = await expenseService.AddAsync("u1", group.Id, new CreateExpenseRequest(
            "Groceries",
            50m,
            "u1",
            SplitType.Equal,
            null,
            null));

        Assert.That(expense.Amount, Is.EqualTo(50m));
        Assert.That(expense.Splits, Has.Count.EqualTo(2));
        Assert.That(expense.Splits.Sum(s => s.Amount), Is.EqualTo(50m));
    }

    [Test]
    public async Task ExpenseService_AddExactExpense_InvalidSum_ThrowsValidationFailedException()
    {
        var group = new Group { Name = "Flat", CreatedByUserId = "u1" };
        group.Members.Add(new GroupMember { UserId = "u1" });
        group.Members.Add(new GroupMember { UserId = "u2" });
        db.Groups.Add(group);
        await db.SaveChangesAsync();

        var expenseService = new ExpenseService(db, userDirectory);
        var request = new CreateExpenseRequest(
            "Dinner",
            100m,
            "u1",
            SplitType.Exact,
            null,
            new[]
            {
                new ExactSplitInput("u1", 40m),
                new ExactSplitInput("u2", 50m) // sum is 90 != 100
            });

        Assert.ThrowsAsync<ValidationFailedException>(async () => await expenseService.AddAsync("u1", group.Id, request));
    }

    [Test]
    public async Task BalanceService_Settlement_CannotSettleWithSelf()
    {
        var group = new Group { Name = "Flat", CreatedByUserId = "u1" };
        group.Members.Add(new GroupMember { UserId = "u1" });
        db.Groups.Add(group);
        await db.SaveChangesAsync();

        var balanceService = new BalanceService(db, userDirectory);
        var request = new CreateSettlementRequest("u1", 10m);

        Assert.ThrowsAsync<ValidationFailedException>(async () => await balanceService.AddSettlementAsync("u1", group.Id, request));
    }
}
