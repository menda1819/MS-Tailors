using Microsoft.Extensions.Logging;
using MSTailors.Application.Auth.Dtos;
using MSTailors.Application.Common.Exceptions;
using MSTailors.Application.Common.Repositories;
using MSTailors.Domain.Entities;
using MSTailors.Domain.ValueObjects;

namespace MSTailors.Application.Auth;

public sealed class AuthService : IAuthService
{
    /// <summary>How long a password-reset token stays valid.</summary>
    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromHours(1);

    private readonly ICustomerRepository _customers;
    private readonly ITokenService _tokens;
    private readonly IPasswordHashingService _passwords;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ICustomerRepository customers,
        ITokenService tokens,
        IPasswordHashingService passwords,
        ILogger<AuthService> logger)
    {
        _customers = customers;
        _tokens = tokens;
        _passwords = passwords;
        _logger = logger;
    }

    public async Task<AuthResult> RegisterAsync(
        RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (!await _customers.IsEmailAvailableAsync(email, cancellationToken: cancellationToken))
        {
            throw new ConflictException("An account with that email address already exists.");
        }

        var customer = new Customer
        {
            Email = email,
            FullName = request.FullName.Trim(),
            Phone = request.Phone.Trim(),
            Role = Domain.Enums.UserRole.Customer
        };

        customer.PasswordHash = _passwords.Hash(customer, request.Password);

        var created = await _customers.AddAsync(customer, cancellationToken);
        _logger.LogInformation("Registered customer {CustomerId}", created.Id);

        return await IssueAsync(created, cancellationToken);
    }

    public async Task<AuthResult> LoginAsync(
        LoginRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _customers.GetByEmailAsync(request.Email, cancellationToken);

        // Same exception whether the account is missing, inactive, or the
        // password is wrong. Distinguishing them would leak which emails exist.
        if (customer is null || !customer.IsActive)
        {
            throw new AuthenticationFailedException();
        }

        if (!_passwords.Verify(customer, customer.PasswordHash, request.Password, out var needsRehash))
        {
            throw new AuthenticationFailedException();
        }

        if (needsRehash)
        {
            // The hasher's work factor moved on; upgrade transparently at login.
            customer.PasswordHash = _passwords.Hash(customer, request.Password);
        }

        customer.LastLoginAt = DateTime.UtcNow;
        return await IssueAsync(customer, cancellationToken);
    }

    public async Task<AuthResult> RefreshAsync(
        string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new AuthenticationFailedException("No refresh token was supplied.");
        }

        var hash = _tokens.HashToken(refreshToken);
        var customer = await _customers.GetByRefreshTokenAsync(hash, cancellationToken)
            ?? throw new AuthenticationFailedException("That refresh token is not valid.");

        var stored = customer.RefreshTokens.FirstOrDefault(token => token.Token == hash);

        if (stored is null || stored.IsExpired)
        {
            throw new AuthenticationFailedException("That refresh token has expired.");
        }

        // Rotation: the presented token is consumed and cannot be replayed.
        customer.RefreshTokens.Remove(stored);

        return await IssueAsync(customer, cancellationToken);
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var hash = _tokens.HashToken(refreshToken);
        var customer = await _customers.GetByRefreshTokenAsync(hash, cancellationToken);

        if (customer is null)
        {
            return;
        }

        customer.RefreshTokens.RemoveAll(token => token.Token == hash);
        await _customers.UpdateAsync(customer, cancellationToken);
    }

    public async Task ForgotPasswordAsync(
        ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _customers.GetByEmailAsync(request.Email, cancellationToken);

        if (customer is null || !customer.IsActive)
        {
            // Return silently. Telling the caller the address is unknown turns
            // this endpoint into an account-enumeration oracle.
            _logger.LogInformation("Password reset requested for an unknown address");
            return;
        }

        var (raw, hash, _) = _tokens.CreateRefreshToken();

        customer.PasswordResetTokenHash = hash;
        customer.PasswordResetExpiresAt = DateTime.UtcNow.Add(ResetTokenLifetime);
        await _customers.UpdateAsync(customer, cancellationToken);

        // NOTE: email delivery is not wired up. The token is logged at Debug so
        // the flow is exercisable in development; it is never returned in the
        // HTTP response, which would hand anyone a password reset for any
        // account. Sending this by email is outstanding work.
        _logger.LogDebug("Password reset token for {CustomerId}: {Token}", customer.Id, raw);
        _logger.LogInformation("Password reset token issued for {CustomerId}", customer.Id);
    }

    public async Task ResetPasswordAsync(
        ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _customers.GetByEmailAsync(request.Email, cancellationToken);

        if (customer is null
            || customer.PasswordResetTokenHash is null
            || customer.PasswordResetExpiresAt is null
            || customer.PasswordResetExpiresAt <= DateTime.UtcNow
            || customer.PasswordResetTokenHash != _tokens.HashToken(request.Token))
        {
            throw new BusinessRuleException("That password reset link is invalid or has expired.");
        }

        customer.PasswordHash = _passwords.Hash(customer, request.NewPassword);
        customer.PasswordResetTokenHash = null;
        customer.PasswordResetExpiresAt = null;

        // Every existing session is invalidated: a password reset usually means
        // the account may have been compromised.
        customer.RefreshTokens.Clear();

        await _customers.UpdateAsync(customer, cancellationToken);
        _logger.LogInformation("Password reset completed for {CustomerId}", customer.Id);
    }

    private async Task<AuthResult> IssueAsync(Customer customer, CancellationToken cancellationToken)
    {
        var (accessToken, accessExpiry) = _tokens.CreateAccessToken(customer);
        var (rawRefresh, refreshHash, refreshExpiry) = _tokens.CreateRefreshToken();

        // Drop expired entries so the array cannot grow without bound.
        customer.RefreshTokens.RemoveAll(token => token.IsExpired);
        customer.RefreshTokens.Add(new RefreshTokenEntry { Token = refreshHash, ExpiresAt = refreshExpiry });

        await _customers.UpdateAsync(customer, cancellationToken);

        var response = new AuthResponse(accessToken, accessExpiry, new AuthenticatedUserDto(
            customer.Id, customer.FullName, customer.Email, customer.Phone,
            customer.Role.ToString(), customer.EmailVerified));

        return new AuthResult(response, rawRefresh, refreshExpiry);
    }
}
