using Microsoft.AspNetCore.Mvc;
using MSTailors.Application.Media;

namespace MSTailors.Api.Controllers.Admin;

public sealed record UploadedMediaDto(
    string Key,
    string Url,
    int Width,
    int Height,
    IReadOnlyList<MediaRendition> Renditions);

[Route("api/admin/media")]
public sealed class AdminMediaController : AdminControllerBase
{
    private readonly IMediaStore _media;

    public AdminMediaController(IMediaStore media) => _media = media;

    /// <summary>
    /// Uploads an image and returns every stored rendition.
    /// </summary>
    /// <remarks>
    /// The file is decoded before anything is written, so a non-image is
    /// rejected regardless of its extension or declared content type.
    /// </remarks>
    [HttpPost]
    [RequestSizeLimit(UploadPolicy.MaxBytes)]
    [ProducesResponseType(typeof(UploadedMediaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<UploadedMediaDto>> Upload(
        IFormFile file,
        [FromQuery] string folder,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "No file was uploaded",
                Detail = "Attach an image file to the 'file' field."
            });
        }

        if (file.Length > UploadPolicy.MaxBytes)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new ProblemDetails
            {
                Status = StatusCodes.Status413PayloadTooLarge,
                Title = "That image is too large",
                Detail = $"Images must be under {UploadPolicy.MaxBytes / (1024 * 1024)} MB."
            });
        }

        await using var stream = file.OpenReadStream();

        var stored = await _media.SaveImageAsync(stream, file.FileName, folder, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new UploadedMediaDto(
            stored.Key, stored.Url, stored.Width, stored.Height, stored.Renditions));
    }

    /// <summary>Removes an image and every rendition of it.</summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([FromQuery] string key, CancellationToken cancellationToken)
    {
        await _media.DeleteAsync(key, cancellationToken);
        return NoContent();
    }
}
