using MSTailors.Application.Common.Dtos;
using MSTailors.Application.Common.Exceptions;
using MSTailors.Application.Common.Repositories;
using MSTailors.Application.Content.Dtos;
using MSTailors.Domain.Entities;
using MSTailors.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MSTailors.Application.Content;

public interface IServiceCatalogService
{
    Task<PagedResponse<ServiceListItemDto>> ListAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<ServiceDetailDto> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
}

public interface IGalleryService
{
    Task<PagedResponse<GalleryItemDto>> ListAsync(
        PageRequest request, GalleryCategory? category, CancellationToken cancellationToken = default);
}

public interface ITestimonialService
{
    Task<PagedResponse<TestimonialDto>> ListAsync(PageRequest request, CancellationToken cancellationToken = default);
}

public interface IBlogService
{
    Task<PagedResponse<BlogPostListItemDto>> ListAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<BlogPostDetailDto> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
}

public interface IInquiryService
{
    Task<InquiryCreatedDto> CreateAsync(CreateInquiryRequest request, CancellationToken cancellationToken = default);
}

public sealed class ServiceCatalogService : IServiceCatalogService
{
    private readonly ISlugRepository<Service> _services;

    public ServiceCatalogService(ISlugRepository<Service> services) => _services = services;

    public async Task<PagedResponse<ServiceListItemDto>> ListAsync(
        PageRequest request, CancellationToken cancellationToken = default)
    {
        var page = await _services.PageAsync(
            request.Page, request.PageSize, service => service.IsActive, cancellationToken);

        var ordered = page.Items.OrderBy(service => service.DisplayOrder).ToList();

        return PagedResponse<ServiceListItemDto>.From(page with { Items = ordered }, service =>
            new ServiceListItemDto(service.Id, service.Name, service.Slug, service.Category,
                service.ShortDescription, service.ImageUrl, service.DisplayOrder));
    }

    public async Task<ServiceDetailDto> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var service = await _services.GetBySlugAsync(slug, cancellationToken);

        if (service is null || !service.IsActive)
        {
            throw new NotFoundException("Service", slug);
        }

        return new ServiceDetailDto(service.Id, service.Name, service.Slug, service.Category,
            service.ShortDescription, service.LongDescription, service.ImageUrl,
            service.BulletPoints, service.DisplayOrder);
    }
}

public sealed class GalleryService : IGalleryService
{
    private readonly IRepository<GalleryItem> _gallery;

    public GalleryService(IRepository<GalleryItem> gallery) => _gallery = gallery;

    public async Task<PagedResponse<GalleryItemDto>> ListAsync(
        PageRequest request, GalleryCategory? category, CancellationToken cancellationToken = default)
    {
        var page = category is { } value
            ? await _gallery.PageAsync(request.Page, request.PageSize,
                item => item.IsActive && item.Category == value, cancellationToken)
            : await _gallery.PageAsync(request.Page, request.PageSize,
                item => item.IsActive, cancellationToken);

        var ordered = page.Items.OrderBy(item => item.DisplayOrder).ToList();

        return PagedResponse<GalleryItemDto>.From(page with { Items = ordered }, item =>
            new GalleryItemDto(item.Id, item.ImageUrl, item.ThumbnailUrl, item.Caption,
                item.AltText, item.Category, item.Category.ToDisplayName(), item.DisplayOrder));
    }
}

public sealed class TestimonialService : ITestimonialService
{
    private readonly IRepository<Testimonial> _testimonials;

    public TestimonialService(IRepository<Testimonial> testimonials) => _testimonials = testimonials;

    public async Task<PagedResponse<TestimonialDto>> ListAsync(
        PageRequest request, CancellationToken cancellationToken = default)
    {
        // Unapproved testimonials are never public — approval is the whole point.
        var page = await _testimonials.PageAsync(request.Page, request.PageSize,
            testimonial => testimonial.IsActive && testimonial.IsApproved, cancellationToken);

        var ordered = page.Items.OrderBy(testimonial => testimonial.DisplayOrder).ToList();

        return PagedResponse<TestimonialDto>.From(page with { Items = ordered }, testimonial =>
            new TestimonialDto(testimonial.Id, testimonial.CustomerName, testimonial.CustomerTitle,
                testimonial.Quote, testimonial.Rating, testimonial.ImageUrl, testimonial.DisplayOrder));
    }
}

public sealed class BlogService : IBlogService
{
    private readonly ISlugRepository<BlogPost> _posts;

    public BlogService(ISlugRepository<BlogPost> posts) => _posts = posts;

    public async Task<PagedResponse<BlogPostListItemDto>> ListAsync(
        PageRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // Published AND not future-dated, so scheduled posts stay hidden.
        var page = await _posts.PageAsync(request.Page, request.PageSize,
            post => post.IsPublished && post.PublishedAt != null && post.PublishedAt <= now,
            cancellationToken);

        var ordered = page.Items.OrderByDescending(post => post.PublishedAt).ToList();

        return PagedResponse<BlogPostListItemDto>.From(page with { Items = ordered }, post =>
            new BlogPostListItemDto(post.Id, post.Title, post.Slug, post.Excerpt,
                post.CoverImageUrl, post.Author, post.Tags, post.PublishedAt));
    }

    public async Task<BlogPostDetailDto> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var post = await _posts.GetBySlugAsync(slug, cancellationToken);

        if (post is null || !post.IsPublished || post.PublishedAt is null || post.PublishedAt > DateTime.UtcNow)
        {
            throw new NotFoundException("Blog post", slug);
        }

        return new BlogPostDetailDto(post.Id, post.Title, post.Slug, post.Excerpt, post.Content,
            post.CoverImageUrl, post.Author, post.Tags, post.PublishedAt);
    }
}

public sealed class InquiryService : IInquiryService
{
    private readonly IRepository<Inquiry> _inquiries;
    private readonly ILogger<InquiryService> _logger;

    public InquiryService(IRepository<Inquiry> inquiries, ILogger<InquiryService> logger)
    {
        _inquiries = inquiries;
        _logger = logger;
    }

    public async Task<InquiryCreatedDto> CreateAsync(
        CreateInquiryRequest request, CancellationToken cancellationToken = default)
    {
        // Honeypot: a filled hidden field means a bot. Answer exactly as a real
        // submission would, so the bot gets no signal that it was caught, but
        // write nothing to the database.
        if (!string.IsNullOrWhiteSpace(request.Website))
        {
            _logger.LogInformation("Discarded a honeypot inquiry submission");
            return new InquiryCreatedDto(string.Empty, "Thank you. We will be in touch shortly.");
        }

        var inquiry = new Inquiry
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Phone = request.Phone.Trim(),
            ServiceNeeded = request.ServiceNeeded,
            Message = request.Message.Trim(),
            PreferredDate = request.PreferredDate,
            Status = InquiryStatus.New
        };

        var created = await _inquiries.AddAsync(inquiry, cancellationToken);

        _logger.LogInformation("Inquiry {InquiryId} received for {Service}",
            created.Id, request.ServiceNeeded);

        return new InquiryCreatedDto(created.Id, "Thank you. We will be in touch shortly.");
    }
}
