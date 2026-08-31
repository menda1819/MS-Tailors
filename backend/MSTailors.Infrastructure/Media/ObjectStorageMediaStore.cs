using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MSTailors.Application.Media;

namespace MSTailors.Infrastructure.Media;

/// <summary>
/// Stores uploads in Cloudflare R2, or any S3-compatible object store.
/// </summary>
/// <remarks>
/// R2 is S3-compatible, so the AWS SDK works against it unchanged given a
/// custom ServiceUrl. R2 charges nothing for egress, and the renditions are
/// generated here at upload time, so serving images costs effectively nothing
/// however much traffic the shop gets.
/// </remarks>
public sealed class ObjectStorageMediaStore : IMediaStore
{
    private readonly IAmazonS3 _client;
    private readonly MediaSettings _settings;
    private readonly ILogger<ObjectStorageMediaStore> _logger;

    public ObjectStorageMediaStore(
        IAmazonS3 client,
        IOptions<MediaSettings> settings,
        ILogger<ObjectStorageMediaStore> logger)
    {
        _client = client;
        _settings = settings.Value;
        _logger = logger;
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

            using var buffer = new MemoryStream(rendition.Bytes, writable: false);

            await _client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _settings.Bucket,
                Key = key,
                InputStream = buffer,
                ContentType = rendition.ContentType,
                DisablePayloadSigning = true,
                // Renditions are immutable — the key carries a random suffix, so
                // a changed image is a new key. A year of caching is safe.
                Headers = { CacheControl = "public, max-age=31536000, immutable" }
            }, cancellationToken);

            var url = $"{_settings.PublicBaseUrl.TrimEnd('/')}/{key}";
            stored.Add(new MediaRendition(url, rendition.Width, rendition.Height, rendition.Bytes.Length));

            if (rendition.Width > largestWidth)
            {
                largestWidth = rendition.Width;
                largestHeight = rendition.Height;
                largestUrl = url;
            }
        }

        _logger.LogInformation(
            "Stored {Count} renditions for {Key} in bucket {Bucket}", stored.Count, slug, _settings.Bucket);

        return new StoredMedia(slug, largestUrl!, largestWidth, largestHeight, stored);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        MediaKey.EnsureSafe(key);

        var listed = await _client.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = _settings.Bucket,
            Prefix = key
        }, cancellationToken);

        if (listed.S3Objects.Count == 0)
        {
            return;
        }

        await _client.DeleteObjectsAsync(new DeleteObjectsRequest
        {
            BucketName = _settings.Bucket,
            Objects = [.. listed.S3Objects.Select(o => new KeyVersion { Key = o.Key })]
        }, cancellationToken);

        _logger.LogInformation("Deleted {Count} objects under {Key}", listed.S3Objects.Count, key);
    }
}
