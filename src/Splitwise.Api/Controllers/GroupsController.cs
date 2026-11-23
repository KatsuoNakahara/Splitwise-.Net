using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Splitwise.Api.Auth;
using Splitwise.Application.Abstractions;
using Splitwise.Application.Dtos;

namespace Splitwise.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/groups")]
public sealed class GroupsController : ControllerBase
{
    private readonly IGroupService groupService;

    public GroupsController(IGroupService groupService)
    {
        this.groupService = groupService;
    }

    [HttpPost]
    public async Task<ActionResult<GroupDetailsDto>> Create([FromBody] CreateGroupRequest request, CancellationToken cancellationToken)
    {
        GroupDetailsDto group = await this.groupService.CreateAsync(this.User.GetUserId(), request, cancellationToken);
        return this.CreatedAtAction(nameof(this.Get), new { groupId = group.Id }, group);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GroupSummaryDto>>> GetMine(CancellationToken cancellationToken)
    {
        return this.Ok(await this.groupService.GetUserGroupsAsync(this.User.GetUserId(), cancellationToken));
    }

    [HttpGet("{groupId:int}")]
    public async Task<ActionResult<GroupDetailsDto>> Get(int groupId, CancellationToken cancellationToken)
    {
        return this.Ok(await this.groupService.GetAsync(this.User.GetUserId(), groupId, cancellationToken));
    }

    [HttpPost("{groupId:int}/members")]
    public async Task<ActionResult<GroupDetailsDto>> AddMember(int groupId, [FromBody] AddMemberRequest request, CancellationToken cancellationToken)
    {
        return this.Ok(await this.groupService.AddMemberAsync(this.User.GetUserId(), groupId, request, cancellationToken));
    }
}
