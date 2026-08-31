using System.ComponentModel.DataAnnotations;

namespace MSTailors.Infrastructure.Configuration;

/// <summary>
/// Bound from the "MongoDb" configuration section. In development these come
/// from .NET user secrets; in production from environment variables
/// (MongoDb__ConnectionString, MongoDb__DatabaseName). Never from a committed file.
/// </summary>
public sealed class MongoDbSettings
{
    public const string SectionName = "MongoDb";

    [Required(AllowEmptyStrings = false, ErrorMessage =
        "MongoDb__ConnectionString is not configured. Set it with: dotnet user-secrets set \"MongoDb__ConnectionString\" \"<uri>\"")]
    public string ConnectionString { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string DatabaseName { get; set; } = "mstailors";

    /// <summary>Server selection timeout. Kept short so a bad URI fails fast instead of hanging a request.</summary>
    [Range(1, 120)]
    public int ServerSelectionTimeoutSeconds { get; set; } = 5;
}
