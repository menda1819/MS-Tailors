using System.ComponentModel.DataAnnotations;

namespace MSTailors.Infrastructure.Media;

public enum MediaStoreProvider
{
    /// <summary>Writes under a local directory. Development only.</summary>
    LocalDisk,

    /// <summary>Cloudflare R2, or any S3-compatible object store.</summary>
    ObjectStorage
}

public sealed class MediaSettings
{
    public const string SectionName = "Media";

    public MediaStoreProvider Provider { get; set; } = MediaStoreProvider.LocalDisk;

    /// <summary>LocalDisk only. Relative paths resolve against the content root.</summary>
    public string LocalRootPath { get; set; } = "uploads";

    /// <summary>
    /// Public base URL that fronts the stored objects — an R2 custom domain, or
    /// the API's own /uploads path in development. No trailing slash.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string PublicBaseUrl { get; set; } = "http://localhost:5001/uploads";

    // ---- Object storage ----------------------------------------------------

    /// <summary>R2: https://&lt;account-id&gt;.r2.cloudflarestorage.com</summary>
    public string? ServiceUrl { get; set; }

    public string? Bucket { get; set; }

    public string? AccessKeyId { get; set; }

    public string? SecretAccessKey { get; set; }

    /// <summary>R2 ignores regions but the S3 client requires one.</summary>
    public string Region { get; set; } = "auto";
}
