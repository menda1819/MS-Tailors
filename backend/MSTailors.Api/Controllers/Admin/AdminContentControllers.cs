using Microsoft.AspNetCore.Mvc;
using MSTailors.Application.Admin;
using MSTailors.Application.Admin.Dtos;
using MSTailors.Application.Common.Dtos;
using MSTailors.Application.Content.Dtos;

namespace MSTailors.Api.Controllers.Admin;

[Route("api/admin/services")]
public sealed class AdminServicesController : AdminControllerBase
{
    private readonly IAdminContentService _content;

    public AdminServicesController(IAdminContentService content) => _content = content;

    [HttpGet]
    public async Task<ActionResult<PagedResponse<ServiceDetailDto>>> List(
        [FromQuery] PageRequest request, CancellationToken cancellationToken)
        => Ok(await _content.ListServicesAsync(request, cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(ServiceDetailDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ServiceDetailDto>> Create(
        [FromBody] ServiceRequest request, CancellationToken cancellationToken)
        => StatusCode(StatusCodes.Status201Created, await _content.CreateServiceAsync(request, cancellationToken));

    [HttpPut("{id}")]
    public async Task<ActionResult<ServiceDetailDto>> Update(
        string id, [FromBody] ServiceRequest request, CancellationToken cancellationToken)
        => Ok(await _content.UpdateServiceAsync(id, request, cancellationToken));

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _content.DeleteServiceAsync(id, cancellationToken);
        return NoContent();
    }
}

[Route("api/admin/gallery")]
public sealed class AdminGalleryController : AdminControllerBase
{
    private readonly IAdminContentService _content;

    public AdminGalleryController(IAdminContentService content) => _content = content;

    [HttpGet]
    public async Task<ActionResult<PagedResponse<GalleryItemDto>>> List(
        [FromQuery] PageRequest request, CancellationToken cancellationToken)
        => Ok(await _content.ListGalleryAsync(request, cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(GalleryItemDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<GalleryItemDto>> Create(
        [FromBody] GalleryItemRequest request, CancellationToken cancellationToken)
        => StatusCode(StatusCodes.Status201Created, await _content.CreateGalleryItemAsync(request, cancellationToken));

    [HttpPut("{id}")]
    public async Task<ActionResult<GalleryItemDto>> Update(
        string id, [FromBody] GalleryItemRequest request, CancellationToken cancellationToken)
        => Ok(await _content.UpdateGalleryItemAsync(id, request, cancellationToken));

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _content.DeleteGalleryItemAsync(id, cancellationToken);
        return NoContent();
    }
}

[Route("api/admin/blog")]
public sealed class AdminBlogController : AdminControllerBase
{
    private readonly IAdminContentService _content;

    public AdminBlogController(IAdminContentService content) => _content = content;

    [HttpGet]
    public async Task<ActionResult<PagedResponse<BlogPostDetailDto>>> List(
        [FromQuery] PageRequest request, CancellationToken cancellationToken)
        => Ok(await _content.ListBlogAsync(request, cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(BlogPostDetailDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<BlogPostDetailDto>> Create(
        [FromBody] BlogPostRequest request, CancellationToken cancellationToken)
        => StatusCode(StatusCodes.Status201Created, await _content.CreateBlogPostAsync(request, cancellationToken));

    [HttpPut("{id}")]
    public async Task<ActionResult<BlogPostDetailDto>> Update(
        string id, [FromBody] BlogPostRequest request, CancellationToken cancellationToken)
        => Ok(await _content.UpdateBlogPostAsync(id, request, cancellationToken));

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _content.DeleteBlogPostAsync(id, cancellationToken);
        return NoContent();
    }
}

[Route("api/admin/testimonials")]
public sealed class AdminTestimonialsController : AdminControllerBase
{
    private readonly IAdminContentService _content;

    public AdminTestimonialsController(IAdminContentService content) => _content = content;

    [HttpGet]
    public async Task<ActionResult<PagedResponse<TestimonialDto>>> List(
        [FromQuery] PageRequest request, CancellationToken cancellationToken)
        => Ok(await _content.ListTestimonialsAsync(request, cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(TestimonialDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<TestimonialDto>> Create(
        [FromBody] TestimonialRequest request, CancellationToken cancellationToken)
        => StatusCode(StatusCodes.Status201Created, await _content.CreateTestimonialAsync(request, cancellationToken));

    [HttpPut("{id}")]
    public async Task<ActionResult<TestimonialDto>> Update(
        string id, [FromBody] TestimonialRequest request, CancellationToken cancellationToken)
        => Ok(await _content.UpdateTestimonialAsync(id, request, cancellationToken));

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _content.DeleteTestimonialAsync(id, cancellationToken);
        return NoContent();
    }
}

[Route("api/admin/coupons")]
public sealed class AdminCouponsController : AdminControllerBase
{
    private readonly IAdminContentService _content;

    public AdminCouponsController(IAdminContentService content) => _content = content;

    [HttpGet]
    public async Task<ActionResult<PagedResponse<CouponDto>>> List(
        [FromQuery] PageRequest request, CancellationToken cancellationToken)
        => Ok(await _content.ListCouponsAsync(request, cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(CouponDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CouponDto>> Create(
        [FromBody] CouponRequest request, CancellationToken cancellationToken)
        => StatusCode(StatusCodes.Status201Created, await _content.CreateCouponAsync(request, cancellationToken));

    [HttpPut("{id}")]
    public async Task<ActionResult<CouponDto>> Update(
        string id, [FromBody] CouponRequest request, CancellationToken cancellationToken)
        => Ok(await _content.UpdateCouponAsync(id, request, cancellationToken));

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _content.DeleteCouponAsync(id, cancellationToken);
        return NoContent();
    }
}
