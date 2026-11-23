using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Splitwise.Api.Auth;
using Splitwise.Application.Abstractions;
using Splitwise.Application.Dtos;

namespace Splitwise.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/groups/{groupId:int}")]
public sealed class BalancesController : ControllerBase
{
    private readonly IBalanceService balanceService;

    public BalancesController(IBalanceService balanceService)
    {
        this.balanceService = balanceService;
    }

    [HttpGet("balances")]
    public async Task<ActionResult<BalanceReportDto>> GetBalances(int groupId, CancellationToken cancellationToken)
    {
        return this.Ok(await this.balanceService.GetBalancesAsync(this.User.GetUserId(), groupId, cancellationToken));
    }

    [HttpPost("settlements")]
    public async Task<ActionResult<SettlementDto>> Settle(int groupId, [FromBody] CreateSettlementRequest request, CancellationToken cancellationToken)
    {
        SettlementDto settlement = await this.balanceService.AddSettlementAsync(this.User.GetUserId(), groupId, request, cancellationToken);
        return this.StatusCode(StatusCodes.Status201Created, settlement);
    }
}
