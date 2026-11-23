using Microsoft.AspNetCore.Mvc;
using Splitwise.Application.Exceptions;

namespace Splitwise.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<ExceptionHandlingMiddleware> logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await this.next(context);
        }
        catch (AppException ex)
        {
            int status = ex switch
            {
                NotFoundException => StatusCodes.Status404NotFound,
                ForbiddenException => StatusCodes.Status403Forbidden,
                UnauthorizedException => StatusCodes.Status401Unauthorized,
                ValidationFailedException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status400BadRequest,
            };

            await WriteAsync(context, status, ex.Message, (ex as ValidationFailedException)?.Errors);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unhandled exception while processing {Path}.", context.Request.Path.Value);
            await WriteAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null);
        }
    }

    private static async Task WriteAsync(HttpContext context, int status, string detail, IReadOnlyList<string>? errors)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = ReasonPhrase(status),
            Detail = detail,
        };

        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem);
    }

    private static string ReasonPhrase(int status)
    {
        return status switch
        {
            StatusCodes.Status400BadRequest => "Bad Request",
            StatusCodes.Status401Unauthorized => "Unauthorized",
            StatusCodes.Status403Forbidden => "Forbidden",
            StatusCodes.Status404NotFound => "Not Found",
            _ => "Internal Server Error",
        };
    }
}
