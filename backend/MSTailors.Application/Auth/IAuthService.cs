using MSTailors.Application.Auth.Dtos;

namespace MSTailors.Application.Auth;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Always succeeds from the caller's point of view, whether or not the email
    /// exists — otherwise the endpoint becomes an account-enumeration oracle.
    /// </summary>
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}
