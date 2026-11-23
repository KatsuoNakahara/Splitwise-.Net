using Microsoft.EntityFrameworkCore;
using Splitwise.Application.Abstractions;
using Splitwise.Application.Dtos;
using Splitwise.Application.Exceptions;
using Splitwise.Domain.Entities;

namespace Splitwise.Application.Services;

public sealed class GroupService : IGroupService
{
    private readonly IAppDbContext db;
    private readonly IUserDirectory users;

    public GroupService(IAppDbContext db, IUserDirectory users)
    {
        this.db = db;
        this.users = users;
    }

    public async Task<GroupDetailsDto> CreateAsync(string userId, CreateGroupRequest request, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> errors = GroupValidator.ValidateCreate(request.Name, request.MemberEmails);

        if (errors.Count > 0)
        {
            throw new ValidationFailedException(errors);
        }

        var memberIds = new HashSet<string> { userId };
        var unknownEmails = new List<string>();

        foreach (string email in (request.MemberEmails ?? Array.Empty<string>()).Select(e => e.Trim()))
        {
            string? id = await this.users.FindUserIdByEmailAsync(email, cancellationToken);

            if (id is null)
            {
                unknownEmails.Add(email);
            }
            else
            {
                _ = memberIds.Add(id);
            }
        }

        if (unknownEmails.Count > 0)
        {
            throw new ValidationFailedException(unknownEmails.Select(e => $"User with email '{e}' was not found.").ToList());
        }

        var group = new Group
        {
            Name = request.Name.Trim(),
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
        };

        foreach (string id in memberIds)
        {
            group.Members.Add(new GroupMember { UserId = id });
        }

        _ = this.db.Groups.Add(group);
        _ = await this.db.SaveChangesAsync(cancellationToken);

        return await this.ToDetailsAsync(group, cancellationToken);
    }

    public async Task<IReadOnlyList<GroupSummaryDto>> GetUserGroupsAsync(string userId, CancellationToken cancellationToken = default)
    {
        List<GroupSummaryDto> groups = await this.db.Groups
            .Where(g => g.Members.Any(m => m.UserId == userId))
            .OrderBy(g => g.Id)
            .Select(g => new GroupSummaryDto(g.Id, g.Name, g.CreatedAt, g.Members.Count))
            .ToListAsync(cancellationToken);

        return groups;
    }

    public async Task<GroupDetailsDto> GetAsync(string userId, int groupId, CancellationToken cancellationToken = default)
    {
        Group group = await GroupAccess.GetForMemberAsync(this.db, userId, groupId, cancellationToken);
        return await this.ToDetailsAsync(group, cancellationToken);
    }

    public async Task<GroupDetailsDto> AddMemberAsync(string userId, int groupId, AddMemberRequest request, CancellationToken cancellationToken = default)
    {
        Group group = await GroupAccess.GetForMemberAsync(this.db, userId, groupId, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ValidationFailedException(new[] { "Email is required." });
        }

        string? newUserId = await this.users.FindUserIdByEmailAsync(request.Email.Trim(), cancellationToken);

        if (newUserId is null)
        {
            throw new ValidationFailedException(new[] { $"User with email '{request.Email.Trim()}' was not found." });
        }

        if (group.Members.Any(m => m.UserId == newUserId))
        {
            throw new ValidationFailedException(new[] { "The user is already a member of this group." });
        }

        if (group.Members.Count >= GroupValidator.MaxMembers)
        {
            throw new ValidationFailedException(new[] { $"A group cannot have more than {GroupValidator.MaxMembers} members." });
        }

        var member = new GroupMember { GroupId = groupId, UserId = newUserId };
        _ = this.db.GroupMembers.Add(member);
        _ = await this.db.SaveChangesAsync(cancellationToken);

        return await this.ToDetailsAsync(group, cancellationToken);
    }

    private async Task<GroupDetailsDto> ToDetailsAsync(Group group, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, string> names = await this.users.GetDisplayNamesAsync(
            group.Members.Select(m => m.UserId),
            cancellationToken);

        List<MemberDto> members = group.Members
            .Select(m => new MemberDto(m.UserId, names.GetValueOrDefault(m.UserId, m.UserId)))
            .OrderBy(m => m.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new GroupDetailsDto(group.Id, group.Name, group.CreatedByUserId, group.CreatedAt, members);
    }
}
