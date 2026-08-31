namespace MSTailors.Application.Media;

/// <summary>
/// What the API will accept as an image upload, and the sizes it produces.
/// </summary>
public static class UploadPolicy
{
    /// <summary>10 MB. Generous for a camera JPEG, small enough to bound memory.</summary>
    public const long MaxBytes = 10 * 1024 * 1024;

    /// <summary>
    /// Widths generated for every upload. Chosen to line up with the layout's
    /// breakpoints so the browser rarely downloads more pixels than it paints.
    /// </summary>
    public static readonly int[] RenditionWidths = [400, 800, 1200, 1600];

    /// <summary>
    /// Extensions accepted. The extension is a hint only — the actual check is
    /// that the decoder can read the bytes, since a file name proves nothing.
    /// </summary>
    public static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".avif"];

    public static readonly string[] AllowedFolders =
        ["products", "brands", "categories", "services", "gallery", "blog", "testimonials"];

    public static bool IsAllowedFolder(string folder) =>
        AllowedFolders.Contains(folder, StringComparer.OrdinalIgnoreCase);
}
