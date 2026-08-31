using System.ComponentModel.DataAnnotations;

namespace MSTailors.Infrastructure.Seeding;

/// <summary>
/// Bound from the "Seed" configuration section.
/// </summary>
/// <remarks>
/// The admin password is read from configuration — user secrets in development,
/// environment variables in production — and is never hardcoded or defaulted.
/// Seeding fails loudly rather than inventing a password.
/// </remarks>
public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    [Required(AllowEmptyStrings = false)]
    [EmailAddress]
    public string AdminEmail { get; set; } = "admin@mstailors.lk";

    [Required(AllowEmptyStrings = false, ErrorMessage =
        "Seed:AdminPassword is not configured. In development: " +
        "dotnet user-secrets set \"Seed:AdminPassword\" \"<password>\" (note the colon). " +
        "In production set the Seed__AdminPassword environment variable.")]
    [MinLength(12, ErrorMessage = "The seeded admin password must be at least 12 characters.")]
    public string AdminPassword { get; set; } = string.Empty;

    public string AdminFullName { get; set; } = "MS Tailors Administrator";

    public string AdminPhone { get; set; } = "+94112345678";
}
