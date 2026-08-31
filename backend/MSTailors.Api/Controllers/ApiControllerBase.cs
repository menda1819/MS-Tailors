using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MSTailors.Application.Carts;

namespace MSTailors.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Cookie carrying the anonymous cart id for signed-out visitors.</summary>
    public const string AnonymousCartCookie = "mst_cart";

    /// <summary>Cookie carrying the refresh token. HttpOnly — script can never read it.</summary>
    public const string RefreshTokenCookie = "mst_refresh";

    /// <summary>Customer id from the validated JWT, or null when anonymous.</summary>
    protected string? CurrentCustomerId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    protected string CurrentUserLabel =>
        User.FindFirstValue(ClaimTypes.Email) ?? CurrentCustomerId ?? "system";

    /// <summary>
    /// Resolves who owns the cart: the signed-in customer, else the anonymous
    /// cookie. A new anonymous id is minted and set when there is neither.
    /// </summary>
    protected CartOwner ResolveCartOwner()
    {
        if (CurrentCustomerId is { Length: > 0 } customerId)
        {
            return new CartOwner(customerId, null);
        }

        if (Request.Cookies.TryGetValue(AnonymousCartCookie, out var existing)
            && !string.IsNullOrWhiteSpace(existing))
        {
            return new CartOwner(null, existing);
        }

        var anonymousId = Guid.NewGuid().ToString("N");

        // The environment comes from IWebHostEnvironment, not a process
        // variable: a test host configured with UseEnvironment("Development")
        // sets no process variable, and marking the cookie Secure over plain
        // HTTP makes the client silently drop it.
        var environment = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();

        Response.Cookies.Append(AnonymousCartCookie, anonymousId, new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(30),
            Path = "/"
        });

        return new CartOwner(null, anonymousId);
    }
}
