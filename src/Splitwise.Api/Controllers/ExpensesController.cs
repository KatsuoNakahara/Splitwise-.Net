using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Splitwise.Api.Auth;
using Splitwise.Application.Abstractions;
using Splitwise.Application.Dtos;

namespace Splitwise.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/groups/{groupId:int}/expenses")]
public sealed class ExpensesController : ControllerBase
{
    private readonly IExpenseService expenseService;

    public ExpensesController(IExpenseService expenseService)
    {
        this.expenseService = expenseService;
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseDto>> Add(int groupId, [FromBody] CreateExpenseRequest request, CancellationToken cancellationToken)
    {
        ExpenseDto expense = await this.expenseService.AddAsync(this.User.GetUserId(), groupId, request, cancellationToken);
        return this.StatusCode(StatusCodes.Status201Created, expense);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExpenseDto>>> List(int groupId, CancellationToken cancellationToken)
    {
        return this.Ok(await this.expenseService.ListAsync(this.User.GetUserId(), groupId, cancellationToken));
    }
}
