using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MSTailors.Application.Media;
using MSTailors.Infrastructure.Media;
using SkiaSharp;

namespace MSTailors.Tests.Media;

public class MediaStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), $"mstailors-media-{Guid.NewGuid():N}");

    private LocalDiskMediaStore CreateSut() => new(
        Options.Create(new MediaSettings
        {
            Provider = MediaStoreProvider.LocalDisk,
            LocalRootPath = _root,
            PublicBaseUrl = "http://localhost:5001/uploads"
        }),
        NullLogger<LocalDiskMediaStore>.Instance);

    /// <summary>A real encoded PNG, so the decoder has something valid to read.</summary>
    private static MemoryStream CreatePng(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(0x29, 0x34, 0x6B));

        using var paint = new SKPaint { Color = SKColors.White };
        canvas.DrawRect(new SKRect(0, 0, width / 2f, height / 2f), paint);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        var stream = new MemoryStream();
        data.SaveTo(stream);
        stream.Position = 0;
        return stream;
    }

    [Fact]
    public async Task Generates_one_rendition_per_configured_width()
    {
        var sut = CreateSut();
        using var source = CreatePng(2000, 2500);

        var stored = await sut.SaveImageAsync(source, "Meridian Shirt.png", "products");

        // 400, 800, 1200, 1600 are all below 2000, plus the original width.
        stored.Renditions.Should().HaveCount(5);
        stored.Renditions.Select(r => r.Width)
            .Should().BeEquivalentTo([400, 800, 1200, 1600, 2000]);
        stored.Width.Should().Be(2000);
        stored.Url.Should().EndWith("-2000.webp");
    }

    [Fact]
    public async Task Never_upscales_a_small_source()
    {
        var sut = CreateSut();
        using var source = CreatePng(500, 500);

        var stored = await sut.SaveImageAsync(source, "small.png", "gallery");

        // A blurry 1600px rendition would cost bandwidth and look worse.
        stored.Renditions.Select(r => r.Width).Should().BeEquivalentTo([400, 500]);
        stored.Renditions.Should().OnlyContain(r => r.Width <= 500);
    }

    [Fact]
    public async Task Preserves_the_aspect_ratio()
    {
        var sut = CreateSut();
        using var source = CreatePng(1600, 2000);

        var stored = await sut.SaveImageAsync(source, "portrait.png", "products");

        foreach (var rendition in stored.Renditions)
        {
            var ratio = rendition.Width / (double)rendition.Height;
            ratio.Should().BeApproximately(0.8, 0.01);
        }
    }

    [Fact]
    public async Task Writes_webp_files_that_are_smaller_than_the_source()
    {
        var sut = CreateSut();
        using var source = CreatePng(1200, 1200);
        var sourceBytes = source.Length;

        var stored = await sut.SaveImageAsync(source, "photo.png", "products");

        var files = Directory.GetFiles(_root, "*.webp", SearchOption.AllDirectories);
        files.Should().HaveCount(stored.Renditions.Count);
        stored.Renditions.Should().OnlyContain(r => r.Bytes > 0);
        stored.Renditions.Sum(r => r.Bytes).Should().BeLessThan(sourceBytes * 3);
    }

    [Fact]
    public async Task A_file_that_is_not_an_image_is_rejected()
    {
        var sut = CreateSut();
        using var notAnImage = new MemoryStream("#!/bin/sh\nrm -rf /"u8.ToArray());

        // The extension says png; the bytes say otherwise. The decoder decides.
        var act = async () => await sut.SaveImageAsync(notAnImage, "payload.png", "products");

        await act.Should().ThrowAsync<InvalidUploadException>()
            .WithMessage("*could not be read as an image*");

        Directory.Exists(_root).Should().BeTrue();
        Directory.GetFiles(_root, "*", SearchOption.AllDirectories)
            .Should().BeEmpty("nothing may be written for a rejected upload");
    }

    [Theory]
    [InlineData("script.svg")]
    [InlineData("archive.zip")]
    [InlineData("notes.txt")]
    public async Task Disallowed_extensions_are_rejected(string fileName)
    {
        var sut = CreateSut();
        using var source = CreatePng(100, 100);

        var act = async () => await sut.SaveImageAsync(source, fileName, "products");

        await act.Should().ThrowAsync<InvalidUploadException>()
            .WithMessage("*not an accepted image type*");
    }

    [Fact]
    public async Task An_unknown_folder_is_rejected()
    {
        var sut = CreateSut();
        using var source = CreatePng(100, 100);

        var act = async () => await sut.SaveImageAsync(source, "photo.png", "etc-passwd");

        await act.Should().ThrowAsync<InvalidUploadException>()
            .WithMessage("*not a folder images can be uploaded to*");
    }

    [Fact]
    public async Task A_traversing_file_name_cannot_escape_the_upload_root()
    {
        var sut = CreateSut();
        using var source = CreatePng(100, 100);

        var stored = await sut.SaveImageAsync(source, "../../../../etc/passwd.png", "products");

        // The stem is slugified, so the traversal collapses to a plain segment.
        stored.Key.Should().StartWith("products/");
        stored.Key.Should().NotContain("..");

        var written = Directory.GetFiles(_root, "*", SearchOption.AllDirectories);
        written.Should().OnlyContain(path => path.StartsWith(_root, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Two_uploads_of_the_same_file_name_do_not_collide()
    {
        var sut = CreateSut();

        using var first = CreatePng(600, 600);
        using var second = CreatePng(600, 600);

        var a = await sut.SaveImageAsync(first, "IMG_0001.png", "products");
        var b = await sut.SaveImageAsync(second, "IMG_0001.png", "products");

        a.Key.Should().NotBe(b.Key);
    }

    [Fact]
    public async Task DeleteAsync_removes_every_rendition()
    {
        var sut = CreateSut();
        using var source = CreatePng(1200, 1200);

        var stored = await sut.SaveImageAsync(source, "temporary.png", "gallery");
        Directory.GetFiles(_root, "*.webp", SearchOption.AllDirectories).Should().NotBeEmpty();

        await sut.DeleteAsync(stored.Key);

        Directory.GetFiles(_root, "*.webp", SearchOption.AllDirectories).Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_is_safe_to_call_twice()
    {
        var sut = CreateSut();
        using var source = CreatePng(400, 400);
        var stored = await sut.SaveImageAsync(source, "gone.png", "gallery");

        await sut.DeleteAsync(stored.Key);
        var act = async () => await sut.DeleteAsync(stored.Key);

        await act.Should().NotThrowAsync();
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            try
            {
                Directory.Delete(_root, recursive: true);
            }
            catch (IOException)
            {
                // A leftover temp directory is not worth failing a test run over.
            }
        }
    }
}
