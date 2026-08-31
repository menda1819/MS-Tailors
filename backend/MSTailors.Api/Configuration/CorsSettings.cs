namespace MSTailors.Api.Configuration;

/// <summary>
/// Bound from the "Cors" section. Populated from Cors__AllowedOrigins in
/// production. There is deliberately no wildcard fallback.
/// </summary>
public sealed class CorsSettings
{
    public const string SectionName = "Cors";
    public const string PolicyName = "MSTailorsCorsPolicy";

    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}
