namespace Splitwise.Application.Exceptions;

public abstract class AppException : Exception
{
    protected AppException(string message)
        : base(message)
    {
    }
}

public sealed class NotFoundException : AppException
{
    public NotFoundException(string message)
        : base(message)
    {
    }
}

public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message)
        : base(message)
    {
    }
}

public sealed class UnauthorizedException : AppException
{
    public UnauthorizedException(string message)
        : base(message)
    {
    }
}

public sealed class ValidationFailedException : AppException
{
    public ValidationFailedException(IReadOnlyList<string> errors)
        : base(string.Join(" ", errors))
    {
        this.Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }
}
