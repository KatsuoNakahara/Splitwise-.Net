using Microsoft.EntityFrameworkCore;
using Splitwise.Application.Abstractions;
using Splitwise.Application.Exceptions;
using Splitwise.Domain.Entities;

namespace Splitwise.Application.Services;

internal static class GroupAccess
{
    public static async Task<Group> GetForMemberAsync(IAppDbContext db, string userId, int groupId, CancellationToken cancellationToken)
    {
        Group? group = await db.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group is null)
        {
            throw new NotFoundException($"Group {groupId} was not found.");
        }

        if (!group.Members.Any(m => m.UserId == userId))
        {
            throw new ForbiddenException("You are not a member of this group.");
        }

        return group;
    }

    public static bool IsValidMoney(decimal amount)
    {
        return decimal.Round(amount, 2) == amount;
    }
}
