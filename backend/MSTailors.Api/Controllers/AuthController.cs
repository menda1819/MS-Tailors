using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MSTailors.Application.Auth;
using MSTailors.Application.Auth.Dtos;
using MSTailors.Application.Carts;

namespace MSTailors.Api.Controllers;

[Route("api/auth")]
[EnableRateLimiting(RateLimitPolicies.Auth)]
public sealed class AuthController : ApiControllerBase
{
    private readonly IAuthService _auth;
    private readonly ICartService _cart;
    private readonly IWebHostEnvironment _environment;

    public AuthController(IAuthService auth, ICartService cart, IWebHostEnvironment environment)
    {
        _auth = auth;
        _cart = cart;
        _environment = environment;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _auth.RegisterAsync(request, cancellationToken);
        await MergeAnonymousCartAsync(result.Response.User.Id, cancellationToken);
        SetRefreshCookie(result);

        return StatusCode(StatusCodes.Status201Created, result.Response);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _auth.LoginAsync(request, cancellationToken);

        // Carry whatever was in the anonymous basket into the account.
        await MergeAnonymousCartAsync(result.Response.User.Id, cancellationToken);
        SetRefreshCookie(result);

        return Ok(result.Response);
    }

    /// <summary>Exchanges a refresh token for a new access token. The old token is consumed.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(
        [FromBody] RefreshRequest? request, CancellationToken cancellationToken)
    {
        // The cookie is authoritative; the body is a fallback for non-browser clients.
        var token = Request.Cookies.TryGetValue(RefreshTokenCookie, out var cookie) && !string.IsNullOrWhiteSpace(cookie)
            ? cookie
            : request?.RefreshToken;

        var result = await _auth.RefreshAsync(token ?? string.Empty, cancellationToken);
        SetRefreshCookie(result);

        return Ok(result.Response);
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        Request.Cookies.TryGetValue(RefreshTokenCookie, out var token);
        await _auth.LogoutAsync(token, cancellationToken);

        Response.Cookies.Delete(RefreshTokenCookie, CookieOptions());
        return NoContent();
    }

    /// <summary>
    /// Always answers 204, whether or not the address is registered. Anything
    /// else turns this into an account-enumeration oracle.
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await _auth.ForgotPasswordAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _auth.ResetPasswordAsync(request, cancellationToken);
        return NoContent();
    }

    private async Task MergeAnonymousCartAsync(string customerId, CancellationToken cancellationToken)
    {
        if (Request.Cookies.TryGetValue(AnonymousCartCookie, out var anonymousId)
            && !string.IsNullOrWhiteSpace(anonymousId))
        {
            await _cart.MergeOnLoginAsync(anonymousId, customerId, cancellationToken);
            Response.Cookies.Delete(AnonymousCartCookie, CookieOptions());
        }
    }

    private void SetRefreshCookie(AuthResult result) =>
        Response.Cookies.Append(RefreshTokenCookie, result.RefreshToken, new Microsoft.AspNetCore.Http.CookieOptions
        {
            // HttpOnly so XSS cannot read it; Strict so it is not sent
            // cross-site; Secure everywhere except local HTTP development.
            HttpOnly = true,
            Secure = !_environment.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Expires = result.RefreshExpiresAtUtc,
            Path = "/"
        });

    private Microsoft.AspNetCore.Http.CookieOptions CookieOptions() => new()
    {
        HttpOnly = true,
        Secure = !_environment.IsDevelopment(),
        SameSite = SameSiteMode.Strict,
        Path = "/"
    };
}
