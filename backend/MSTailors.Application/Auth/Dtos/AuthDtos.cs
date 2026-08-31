namespace MSTailors.Application.Auth.Dtos;

public sealed class RegisterRequest
{
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public sealed class LoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public sealed class RefreshRequest
{
    /// <summary>Optional in the body — the cookie is the primary carrier.</summary>
    public string? RefreshToken { get; init; }
}

public sealed class ForgotPasswordRequest
{
    public string Email { get; init; } = string.Empty;
}

public sealed class ResetPasswordRequest
{
    public string Email { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}

/// <summary>
/// Returned on register/login/refresh. The refresh token is deliberately not in
/// this DTO — it is set as an HttpOnly cookie by the controller so script on the
/// page can never read it.
/// </summary>
public sealed record AuthResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    AuthenticatedUserDto User);

public sealed record AuthenticatedUserDto(
    string Id,
    string FullName,
    string Email,
    string Phone,
    string Role,
    bool EmailVerified);

/// <summary>Internal: carries the raw refresh token back to the controller.</summary>
public sealed record AuthResult(AuthResponse Response, string RefreshToken, DateTime RefreshExpiresAtUtc);
