namespace MSTailors.Application.Media;

/// <summary>One stored rendition of an uploaded image.</summary>
public sealed record MediaRendition(string Url, int Width, int Height, long Bytes);

/// <summary>
/// The result of an upload: the original plus every generated size.
/// </summary>
/// <remarks>
/// <see cref="Url"/> is the largest rendition and is what gets stored on the
/// entity. The rest exist so a responsive <c>srcset</c> can be built without
/// asking a transformation service for them at request time.
/// </remarks>
public sealed record StoredMedia(
    string Key,
    string Url,
    int Width,
    int Height,
    IReadOnlyList<MediaRendition> Renditions);

/// <summary>
/// Persists uploaded images outside the repository and outside the database.
/// </summary>
/// <remarks>
/// Implemented twice: object storage for real deployments, local disk for
/// development so nobody needs cloud credentials to run the app. Application
/// depends only on this interface.
/// </remarks>
public interface IMediaStore
{
    /// <summary>
    /// Resizes the image into the standard rendition set and stores every one.
    /// </summary>
    /// <param name="content">The uploaded bytes. Not disposed by the store.</param>
    /// <param name="fileName">Original file name, used only to derive an extension.</param>
    /// <param name="folder">Logical grouping — "products", "brands", "gallery".</param>
    Task<StoredMedia> SaveImageAsync(
        Stream content, string fileName, string folder, CancellationToken cancellationToken = default);

    /// <summary>Removes an object and all of its renditions. Safe to call twice.</summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>Thrown when an upload is rejected before anything is stored.</summary>
public sealed class InvalidUploadException : Exception
{
    public InvalidUploadException(string message) : base(message)
    {
    }
}
