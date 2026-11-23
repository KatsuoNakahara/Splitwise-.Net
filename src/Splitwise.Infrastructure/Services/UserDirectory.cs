using Microsoft.EntityFrameworkCore;
using Splitwise.Application.Abstractions;
using Splitwise.Infrastructure.Persistence;

namespace Splitwise.Infrastructure.Services;

public sealed class UserDirectory : IUserDirectory
{
    private readonly AppDbContext db;

    public UserDirectory(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<string?> FindUserIdByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        string normalized = email.Trim().ToUpperInvariant();

        return await this.db.Users
            .Where(u => u.NormalizedEmail == normalized)
            .Select(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetDisplayNamesAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default)
    {
        List<string> ids = userIds.Distinct().ToList();

        return await this.db.Users
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, cancellationToken);
    }
}
