namespace MSTailors.Application.Common.Exceptions;

/// <summary>Requested resource does not exist (or is soft-deleted). Maps to 404.</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string resource, string key)
        : base($"{resource} '{key}' was not found.")
    {
    }

    public NotFoundException(string message) : base(message)
    {
    }
}

/// <summary>Request conflicts with current state — duplicate slug, taken email. Maps to 409.</summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}

/// <summary>Caller is authenticated but not allowed. Maps to 403.</summary>
public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string message = "You do not have access to this resource.") : base(message)
    {
    }
}

/// <summary>Credentials rejected. Maps to 401.</summary>
public sealed class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException(string message = "Invalid email or password.") : base(message)
    {
    }
}

/// <summary>
/// Request is well-formed but cannot be satisfied by current data — out of
/// stock, expired coupon, inactive product. Maps to 422.
/// </summary>
public sealed class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message)
    {
    }

    public BusinessRuleException(string message, IReadOnlyList<string> details) : base(message)
    {
        Details = details;
    }

    public IReadOnlyList<string> Details { get; } = [];
}
