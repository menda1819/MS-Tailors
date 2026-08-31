using MSTailors.Application.Media;
using SkiaSharp;

namespace MSTailors.Infrastructure.Media;

public sealed record RenderedImage(byte[] Bytes, int Width, int Height, string Extension, string ContentType);

/// <summary>
/// Decodes an upload and re-encodes it at the configured widths.
/// </summary>
/// <remarks>
/// Renditions are produced once, at upload time, because an admin uploads
/// rarely and customers view constantly. That removes any need for a runtime
/// transformation service and the per-image fee that comes with one.
///
/// SkiaSharp rather than ImageSharp: ImageSharp requires a paid commercial
/// licence above a revenue threshold, SkiaSharp is MIT.
/// </remarks>
public static class ImageRenditionBuilder
{
    private const int WebpQuality = 82;

    /// <summary>
    /// Decodes the source and renders the standard set, largest first.
    /// </summary>
    /// <exception cref="InvalidUploadException">The bytes are not a readable image.</exception>
    public static IReadOnlyList<RenderedImage> Build(Stream source)
    {
        using var managed = new SKManagedStream(source);

        // The real format check. A file called photo.jpg containing a script
        // fails here, which is the point — the extension is only ever a hint.
        using var original = SKBitmap.Decode(managed)
            ?? throw new InvalidUploadException(
                "That file could not be read as an image. Upload a JPEG, PNG or WebP.");

        if (original.Width < 1 || original.Height < 1)
        {
            throw new InvalidUploadException("That image has no dimensions.");
        }

        var widths = UploadPolicy.RenditionWidths
            // Never upscale: a 500px source produces 400px and itself, not a
            // blurry 1600px rendition that costs bandwidth and looks worse.
            .Where(width => width < original.Width)
            .Append(original.Width)
            .Distinct()
            .OrderByDescending(width => width)
            .ToList();

        var rendered = new List<RenderedImage>(widths.Count);

        foreach (var width in widths)
        {
            var height = (int)Math.Round(original.Height * (width / (double)original.Width));
            rendered.Add(Encode(original, width, Math.Max(height, 1)));
        }

        return rendered;
    }

    private static RenderedImage Encode(SKBitmap source, int width, int height)
    {
        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

        using var resized = source.Resize(info, new SKSamplingOptions(SKCubicResampler.Mitchell))
            ?? throw new InvalidUploadException("That image could not be resized.");

        using var image = SKImage.FromBitmap(resized);
        using var data = image.Encode(SKEncodedImageFormat.Webp, WebpQuality)
            ?? throw new InvalidUploadException("That image could not be encoded.");

        return new RenderedImage(data.ToArray(), width, height, ".webp", "image/webp");
    }
}
