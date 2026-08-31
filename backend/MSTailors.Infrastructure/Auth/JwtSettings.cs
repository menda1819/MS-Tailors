using System.ComponentModel.DataAnnotations;

namespace MSTailors.Infrastructure.Auth;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    [Required(AllowEmptyStrings = false, ErrorMessage =
        "Jwt:Key is not configured. In development: dotnet user-secrets set \"Jwt:Key\" \"<secret>\" " +
        "(note the colon). In production set the Jwt__Key environment variable.")]
    [MinLength(32, ErrorMessage = "Jwt:Key must be at least 32 characters for HMAC-SHA256.")]
    public string Key { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; set; } = "https://localhost:5001";

    [Required(AllowEmptyStrings = false)]
    public string Audience { get; set; } = "mstailors-web";

    [Range(1, 1440)]
    public int AccessTokenMinutes { get; set; } = 15;

    [Range(1, 365)]
    public int RefreshTokenDays { get; set; } = 7;
}
