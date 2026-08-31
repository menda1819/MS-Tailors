using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MSTailors.Application.Media;

namespace MSTailors.Infrastructure.Media;

/// <summary>
/// Stores uploads under a local directory. Development only.
/// </summary>
/// <remarks>
/// Not for production: the directory does not survive a container redeploy, is
/// invisible to a second instance, and has no CDN in front of it. It exists so
/// the app runs without cloud credentials.
/// </remarks>
public sealed class LocalDiskMediaStore : IMediaStore
{
    private readonly MediaSettings _settings;
    private readonly string _root;
    private readonly ILogger<LocalDiskMediaStore> _logger;

    public LocalDiskMediaStore(
        IOptions<MediaSettings> settings,
        ILogger<LocalDiskMediaStore> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        _root = Path.IsPathRooted(_settings.LocalRootPath)
            ? _settings.LocalRootPath
            : Path.Combine(AppContext.BaseDirectory, _settings.LocalRootPath);

        Directory.CreateDirectory(_root);
    }

    public async Task<StoredMedia> SaveImageAsync(
        Stream content, string fileName, string folder, CancellationToken cancellationToken = default)
    {
        MediaKey.Validate(fileName, folder);

        var renditions = ImageRenditionBuilder.Build(content);
        var slug = MediaKey.Create(folder, fileName);

        var stored = new List<MediaRendition>(renditions.Count);
        string? largestUrl = null;
        var largestWidth = 0;
        var largestHeight = 0;

        foreach (var rendition in renditions)
        {
            var key = $"{slug}-{rendition.Width}{rendition.Extension}";
            var path = Path.Combine(_root, key.Replace('/', Path.DirectorySeparatorChar));

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path, rendition.Bytes, cancellationToken);

            var url = $"{_settings.PublicBaseUrl.TrimEnd('/')}/{key}";
            stored.Add(new MediaRendition(url, rendition.Width, rendition.Height, rendition.Bytes.Length));

            if (rendition.Width > largestWidth)
            {
                largestWidth = rendition.Width;
                largestHeight = rendition.Height;
                largestUrl = url;
            }
        }

        _logger.LogInformation("Stored {Count} renditions for {Key} on local disk", stored.Count, slug);

        return new StoredMedia(slug, largestUrl!, largestWidth, largestHeight, stored);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Task.CompletedTask;
        }

        MediaKey.EnsureSafe(key);

        var directory = Path.Combine(_root, Path.GetDirectoryName(key.Replace('/', Path.DirectorySeparatorChar)) ?? string.Empty);

        if (!Directory.Exists(directory))
        {
            return Task.CompletedTask;
        }

        var prefix = Path.GetFileName(key);

        foreach (var file in Directory.EnumerateFiles(directory, $"{prefix}-*"))
        {
            try
            {
                File.Delete(file);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Could not delete {File}", file);
            }
        }

        return Task.CompletedTask;
    }
}

/// <summary>Builds and validates storage keys.</summary>
internal static class MediaKey
{
    public static void Validate(string fileName, string folder)
    {
        if (!UploadPolicy.IsAllowedFolder(folder))
        {
            throw new InvalidUploadException($"'{folder}' is not a folder images can be uploaded to.");
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (!UploadPolicy.AllowedExtensions.Contains(extension))
        {
            throw new InvalidUploadException(
                $"'{extension}' is not an accepted image type. Use JPEG, PNG or WebP.");
        }
    }

    /// <summary>
    /// A key like <c>products/2026/08/meridian-shirt-a1b2c3d4</c>. The random
    /// suffix means two uploads of "IMG_0001.jpg" never collide, and the name is
    /// slugified so a hostile file name cannot escape the folder.
    /// </summary>
    public static string Create(string folder, string fileName)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName);

        var slug = new string(stem
            .ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray())
            .Trim('-');

        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        if (slug.Length == 0)
        {
            slug = "image";
        }

        if (slug.Length > 60)
        {
            slug = slug[..60].TrimEnd('-');
        }

        var now = DateTime.UtcNow;
        var suffix = Guid.NewGuid().ToString("N")[..8];

        return $"{folder.ToLowerInvariant()}/{now:yyyy}/{now:MM}/{slug}-{suffix}";
    }

    /// <summary>Rejects traversal before a key ever reaches the filesystem.</summary>
    public static void EnsureSafe(string key)
    {
        if (key.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(key))
        {
            throw new InvalidUploadException("That media key is not valid.");
        }
    }
}
