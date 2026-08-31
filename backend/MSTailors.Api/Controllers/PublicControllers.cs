using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MSTailors.Application.Catalog;
using MSTailors.Application.Catalog.Dtos;
using MSTailors.Application.Common.Dtos;
using MSTailors.Application.Content;
using MSTailors.Application.Content.Dtos;
using MSTailors.Domain.Enums;

namespace MSTailors.Api.Controllers;

[Route("api/brands")]
public sealed class BrandsController : ApiControllerBase
{
    private readonly IBrandService _brands;

    public BrandsController(IBrandService brands) => _brands = brands;

    /// <summary>Active brands, ordered for display.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<BrandListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<BrandListItemDto>>> List(
        [FromQuery] PageRequest request, CancellationToken cancellationToken)
        => Ok(await _brands.ListAsync(request, cancellationToken));

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(BrandDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BrandDetailDto>> Get(string slug, CancellationToken cancellationToken)
        => Ok(await _brands.GetBySlugAsync(slug, cancellationToken));
}

[Route("api/categories")]
public sealed class CategoriesController : ApiControllerBase
{
    private readonly ICategoryService _categories;

    public CategoriesController(ICategoryService categories) => _categories = categories;

    /// <summary>Top-level categories with their children nested.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> Get(CancellationToken cancellationToken)
        => Ok(await _categories.GetTreeAsync(cancellationToken));
}

[Route("api/products")]
public sealed class ProductsController : ApiControllerBase
{
    private readonly IProductService _products;

    public ProductsController(IProductService products) => _products = products;

    /// <summary>Filtered, sorted, paged shop listing.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ProductListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ProductListItemDto>>> Search(
        [FromQuery] ProductQueryRequest request, CancellationToken cancellationToken)
        => Ok(await _products.SearchAsync(request, cancellationToken));

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDetailDto>> Get(string slug, CancellationToken cancellationToken)
        => Ok(await _products.GetBySlugAsync(slug, cancellationToken));

    /// <summary>Products sharing this one's brand or categories.</summary>
    [HttpGet("{slug}/related")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductListItemDto>>> Related(
        string slug, [FromQuery] int limit = 8, CancellationToken cancellationToken = default)
        => Ok(await _products.GetRelatedAsync(slug, limit, cancellationToken));
}

[Route("api/services")]
public sealed class ServicesController : ApiControllerBase
{
    private readonly IServiceCatalogService _services;

    public ServicesController(IServiceCatalogService services) => _services = services;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ServiceListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ServiceListItemDto>>> List(
        [FromQuery] PageRequest request, CancellationToken cancellationToken)
        => Ok(await _services.ListAsync(request, cancellationToken));

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(ServiceDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceDetailDto>> Get(string slug, CancellationToken cancellationToken)
        => Ok(await _services.GetBySlugAsync(slug, cancellationToken));
}

[Route("api/gallery")]
public sealed class GalleryController : ApiControllerBase
{
    private readonly IGalleryService _gallery;

    public GalleryController(IGalleryService gallery) => _gallery = gallery;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<GalleryItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<GalleryItemDto>>> List(
        [FromQuery] PageRequest request,
        [FromQuery] GalleryCategory? category,
        CancellationToken cancellationToken)
        => Ok(await _gallery.ListAsync(request, category, cancellationToken));
}

[Route("api/testimonials")]
public sealed class TestimonialsController : ApiControllerBase
{
    private readonly ITestimonialService _testimonials;

    public TestimonialsController(ITestimonialService testimonials) => _testimonials = testimonials;

    /// <summary>Approved testimonials only.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<TestimonialDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<TestimonialDto>>> List(
        [FromQuery] PageRequest request, CancellationToken cancellationToken)
        => Ok(await _testimonials.ListAsync(request, cancellationToken));
}

[Route("api/blog")]
public sealed class BlogController : ApiControllerBase
{
    private readonly IBlogService _blog;

    public BlogController(IBlogService blog) => _blog = blog;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<BlogPostListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<BlogPostListItemDto>>> List(
        [FromQuery] PageRequest request, CancellationToken cancellationToken)
        => Ok(await _blog.ListAsync(request, cancellationToken));

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(BlogPostDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BlogPostDetailDto>> Get(string slug, CancellationToken cancellationToken)
        => Ok(await _blog.GetBySlugAsync(slug, cancellationToken));
}

[Route("api/inquiries")]
public sealed class InquiriesController : ApiControllerBase
{
    private readonly IInquiryService _inquiries;

    public InquiriesController(IInquiryService inquiries) => _inquiries = inquiries;

    /// <summary>Submits a tailoring inquiry. Rate limited and honeypot protected.</summary>
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Inquiries)]
    [ProducesResponseType(typeof(InquiryCreatedDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<InquiryCreatedDto>> Create(
        [FromBody] CreateInquiryRequest request, CancellationToken cancellationToken)
    {
        var created = await _inquiries.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }
}
