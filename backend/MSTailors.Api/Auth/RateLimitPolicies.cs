namespace MSTailors.Api.Controllers;

/// <summary>Named rate-limiting policies, referenced by [EnableRateLimiting].</summary>
public static class RateLimitPolicies
{
    /// <summary>Login, register, refresh, password reset — brute-force surface.</summary>
    public const string Auth = "auth";

    /// <summary>Public inquiry form — spam surface.</summary>
    public const string Inquiries = "inquiries";

    /// <summary>Checkout — limits abuse of the transactional path.</summary>
    public const string Checkout = "checkout";
}
