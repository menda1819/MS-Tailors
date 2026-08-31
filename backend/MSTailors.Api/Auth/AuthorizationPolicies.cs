namespace MSTailors.Api.Controllers;

public static class AuthorizationPolicies
{
    /// <summary>Requires the Admin role claim, which is issued only from the database.</summary>
    public const string AdminOnly = "AdminOnly";
}
