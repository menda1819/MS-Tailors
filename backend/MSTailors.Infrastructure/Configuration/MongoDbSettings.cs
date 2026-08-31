using System.ComponentModel.DataAnnotations;

namespace MSTailors.Infrastructure.Configuration;

/// <summary>
/// Bound from the "MongoDb" configuration section.
///
/// Two spellings, and they are not interchangeable:
///   user secrets / appsettings (JSON):  MongoDb:ConnectionString
///   environment variables:              MongoDb__ConnectionString
/// The `__` to `:` translation is an environment-variable convention only. Using
/// `__` with `dotnet user-secrets set` stores a literal key that silently never
/// binds. Never read either from a committed file.
/// </summary>
public sealed class MongoDbSettings
{
    public const string SectionName = "MongoDb";

    [Required(AllowEmptyStrings = false, ErrorMessage =
        "MongoDb:ConnectionString is not configured. In development: " +
        "dotnet user-secrets set \"MongoDb:ConnectionString\" \"<uri>\" " +
        "(note the colon — user secrets are JSON, so the double-underscore form does not bind). " +
        "In production set the MongoDb__ConnectionString environment variable.")]
    public string ConnectionString { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string DatabaseName { get; set; } = "mstailors";

    /// <summary>Server selection timeout. Kept short so a bad URI fails fast instead of hanging a request.</summary>
    [Range(1, 120)]
    public int ServerSelectionTimeoutSeconds { get; set; } = 5;
}
