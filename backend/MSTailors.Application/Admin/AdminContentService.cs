using MSTailors.Application.Admin.Dtos;
using MSTailors.Application.Carts;
using MSTailors.Application.Common.Dtos;
using MSTailors.Application.Common.Exceptions;
using MSTailors.Application.Common.Repositories;
using MSTailors.Application.Content.Dtos;
using MSTailors.Domain.Entities;

namespace MSTailors.Application.Admin;

public interface IAdminContentService
{
    Task<PagedResponse<ServiceDetailDto>> ListServicesAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<ServiceDetailDto> CreateServiceAsync(ServiceRequest request, CancellationToken cancellationToken = default);
    Task<ServiceDetailDto> UpdateServiceAsync(string id, ServiceRequest request, CancellationToken cancellationToken = default);
    Task DeleteServiceAsync(string id, CancellationToken cancellationToken = default);

    Task<PagedResponse<GalleryItemDto>> ListGalleryAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<GalleryItemDto> CreateGalleryItemAsync(GalleryItemRequest request, CancellationToken cancellationToken = default);
    Task<GalleryItemDto> UpdateGalleryItemAsync(string id, GalleryItemRequest request, CancellationToken cancellationToken = default);
    Task DeleteGalleryItemAsync(string id, CancellationToken cancellationToken = default);

    Task<PagedResponse<BlogPostDetailDto>> ListBlogAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<BlogPostDetailDto> CreateBlogPostAsync(BlogPostRequest request, CancellationToken cancellationToken = default);
    Task<BlogPostDetailDto> UpdateBlogPostAsync(string id, BlogPostRequest request, CancellationToken cancellationToken = default);
    Task DeleteBlogPostAsync(string id, CancellationToken cancellationToken = default);

    Task<PagedResponse<TestimonialDto>> ListTestimonialsAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<TestimonialDto> CreateTestimonialAsync(TestimonialRequest request, CancellationToken cancellationToken = default);
    Task<TestimonialDto> UpdateTestimonialAsync(string id, TestimonialRequest request, CancellationToken cancellationToken = default);
    Task DeleteTestimonialAsync(string id, CancellationToken cancellationToken = default);

    Task<PagedResponse<CouponDto>> ListCouponsAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<CouponDto> CreateCouponAsync(CouponRequest request, CancellationToken cancellationToken = default);
    Task<CouponDto> UpdateCouponAsync(string id, CouponRequest request, CancellationToken cancellationToken = default);
    Task DeleteCouponAsync(string id, CancellationToken cancellationToken = default);
}

public sealed class AdminContentService : IAdminContentService
{
    private readonly ISlugRepository<Service> _services;
    private readonly IRepository<GalleryItem> _gallery;
    private readonly ISlugRepository<BlogPost> _blog;
    private readonly IRepository<Testimonial> _testimonials;
    private readonly ICouponRepository _coupons;

    public AdminContentService(
        ISlugRepository<Service> services,
        IRepository<GalleryItem> gallery,
        ISlugRepository<BlogPost> blog,
        IRepository<Testimonial> testimonials,
        ICouponRepository coupons)
    {
        _services = services;
        _gallery = gallery;
        _blog = blog;
        _testimonials = testimonials;
        _coupons = coupons;
    }

    // ---- Services ----------------------------------------------------------

    public async Task<PagedResponse<ServiceDetailDto>> ListServicesAsync(
        PageRequest request, CancellationToken cancellationToken = default)
    {
        var page = await _services.PageAsync(request.Page, request.PageSize, null, cancellationToken);
        return PagedResponse<ServiceDetailDto>.From(page, ToDto);
    }

    public async Task<ServiceDetailDto> CreateServiceAsync(
        ServiceRequest request, CancellationToken cancellationToken = default)
    {
        var slug = Normalise(request.Slug);

        if (!await _services.IsSlugAvailableAsync(slug, null, cancellationToken))
        {
            throw new ConflictException($"The slug '{slug}' is already in use by another service.");
        }

        return ToDto(await _services.AddAsync(Apply(new Service(), request, slug), cancellationToken));
    }

    public async Task<ServiceDetailDto> UpdateServiceAsync(
        string id, ServiceRequest request, CancellationToken cancellationToken = default)
    {
        var service = await _services.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Service", id);

        var slug = Normalise(request.Slug);

        if (!await _services.IsSlugAvailableAsync(slug, id, cancellationToken))
        {
            throw new ConflictException($"The slug '{slug}' is already in use by another service.");
        }

        Apply(service, request, slug);
        await _services.UpdateAsync(service, cancellationToken);

        return ToDto(service);
    }

    public async Task DeleteServiceAsync(string id, CancellationToken cancellationToken = default)
    {
        _ = await _services.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Service", id);
        await _services.SoftDeleteAsync(id, cancellationToken);
    }

    private static Service Apply(Service service, ServiceRequest request, string slug)
    {
        service.Name = request.Name.Trim();
        service.Slug = slug;
        service.Category = request.Category.Trim();
        service.ShortDescription = request.ShortDescription.Trim();
        service.LongDescription = request.LongDescription.Trim();
        service.ImageUrl = request.ImageUrl.Trim();
        service.BulletPoints = [.. request.BulletPoints.Select(point => point.Trim())];
        service.DisplayOrder = request.DisplayOrder;
        service.IsActive = request.IsActive;
        return service;
    }

    private static ServiceDetailDto ToDto(Service service) => new(
        service.Id, service.Name, service.Slug, service.Category, service.ShortDescription,
        service.LongDescription, service.ImageUrl, service.BulletPoints, service.DisplayOrder);

    // ---- Gallery -----------------------------------------------------------

    public async Task<PagedResponse<GalleryItemDto>> ListGalleryAsync(
        PageRequest request, CancellationToken cancellationToken = default)
    {
        var page = await _gallery.PageAsync(request.Page, request.PageSize, null, cancellationToken);
        return PagedResponse<GalleryItemDto>.From(page, ToDto);
    }

    public async Task<GalleryItemDto> CreateGalleryItemAsync(
        GalleryItemRequest request, CancellationToken cancellationToken = default) =>
        ToDto(await _gallery.AddAsync(Apply(new GalleryItem(), request), cancellationToken));

    public async Task<GalleryItemDto> UpdateGalleryItemAsync(
        string id, GalleryItemRequest request, CancellationToken cancellationToken = default)
    {
        var item = await _gallery.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Gallery item", id);

        Apply(item, request);
        await _gallery.UpdateAsync(item, cancellationToken);

        return ToDto(item);
    }

    public async Task DeleteGalleryItemAsync(string id, CancellationToken cancellationToken = default)
    {
        _ = await _gallery.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Gallery item", id);
        await _gallery.SoftDeleteAsync(id, cancellationToken);
    }

    private static GalleryItem Apply(GalleryItem item, GalleryItemRequest request)
    {
        item.ImageUrl = request.ImageUrl.Trim();
        item.ThumbnailUrl = request.ThumbnailUrl.Trim();
        item.Caption = request.Caption.Trim();
        item.AltText = request.AltText.Trim();
        item.Category = request.Category;
        item.DisplayOrder = request.DisplayOrder;
        item.IsActive = request.IsActive;
        return item;
    }

    private static GalleryItemDto ToDto(GalleryItem item) => new(
        item.Id, item.ImageUrl, item.ThumbnailUrl, item.Caption, item.AltText,
        item.Category, MSTailors.Domain.Enums.EnumDisplayExtensions.ToDisplayName(item.Category),
        item.DisplayOrder);

    // ---- Blog --------------------------------------------------------------

    public async Task<PagedResponse<BlogPostDetailDto>> ListBlogAsync(
        PageRequest request, CancellationToken cancellationToken = default)
    {
        var page = await _blog.PageAsync(request.Page, request.PageSize, null, cancellationToken);
        return PagedResponse<BlogPostDetailDto>.From(page, ToDto);
    }

    public async Task<BlogPostDetailDto> CreateBlogPostAsync(
        BlogPostRequest request, CancellationToken cancellationToken = default)
    {
        var slug = Normalise(request.Slug);

        if (!await _blog.IsSlugAvailableAsync(slug, null, cancellationToken))
        {
            throw new ConflictException($"The slug '{slug}' is already in use by another post.");
        }

        return ToDto(await _blog.AddAsync(Apply(new BlogPost(), request, slug), cancellationToken));
    }

    public async Task<BlogPostDetailDto> UpdateBlogPostAsync(
        string id, BlogPostRequest request, CancellationToken cancellationToken = default)
    {
        var post = await _blog.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Blog post", id);

        var slug = Normalise(request.Slug);

        if (!await _blog.IsSlugAvailableAsync(slug, id, cancellationToken))
        {
            throw new ConflictException($"The slug '{slug}' is already in use by another post.");
        }

        Apply(post, request, slug);
        await _blog.UpdateAsync(post, cancellationToken);

        return ToDto(post);
    }

    public async Task DeleteBlogPostAsync(string id, CancellationToken cancellationToken = default)
    {
        _ = await _blog.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Blog post", id);
        await _blog.SoftDeleteAsync(id, cancellationToken);
    }

    private static BlogPost Apply(BlogPost post, BlogPostRequest request, string slug)
    {
        post.Title = request.Title.Trim();
        post.Slug = slug;
        post.Excerpt = request.Excerpt.Trim();
        post.Content = request.Content;
        post.CoverImageUrl = request.CoverImageUrl.Trim();
        post.Author = request.Author.Trim();
        post.Tags = [.. request.Tags];
        post.IsPublished = request.IsPublished;
        // Publishing without an explicit date means "now", so a post cannot be
        // marked published yet stay invisible to the public listing.
        post.PublishedAt = request.PublishedAt ?? (request.IsPublished ? DateTime.UtcNow : null);
        return post;
    }

    private static BlogPostDetailDto ToDto(BlogPost post) => new(
        post.Id, post.Title, post.Slug, post.Excerpt, post.Content,
        post.CoverImageUrl, post.Author, post.Tags, post.PublishedAt);

    // ---- Testimonials ------------------------------------------------------

    public async Task<PagedResponse<TestimonialDto>> ListTestimonialsAsync(
        PageRequest request, CancellationToken cancellationToken = default)
    {
        var page = await _testimonials.PageAsync(request.Page, request.PageSize, null, cancellationToken);
        return PagedResponse<TestimonialDto>.From(page, ToDto);
    }

    public async Task<TestimonialDto> CreateTestimonialAsync(
        TestimonialRequest request, CancellationToken cancellationToken = default) =>
        ToDto(await _testimonials.AddAsync(Apply(new Testimonial(), request), cancellationToken));

    public async Task<TestimonialDto> UpdateTestimonialAsync(
        string id, TestimonialRequest request, CancellationToken cancellationToken = default)
    {
        var testimonial = await _testimonials.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Testimonial", id);

        Apply(testimonial, request);
        await _testimonials.UpdateAsync(testimonial, cancellationToken);

        return ToDto(testimonial);
    }

    public async Task DeleteTestimonialAsync(string id, CancellationToken cancellationToken = default)
    {
        _ = await _testimonials.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Testimonial", id);
        await _testimonials.SoftDeleteAsync(id, cancellationToken);
    }

    private static Testimonial Apply(Testimonial testimonial, TestimonialRequest request)
    {
        testimonial.CustomerName = request.CustomerName.Trim();
        testimonial.CustomerTitle = request.CustomerTitle?.Trim();
        testimonial.Quote = request.Quote.Trim();
        testimonial.Rating = request.Rating;
        testimonial.ImageUrl = request.ImageUrl?.Trim();
        testimonial.IsApproved = request.IsApproved;
        testimonial.DisplayOrder = request.DisplayOrder;
        testimonial.IsActive = request.IsActive;
        return testimonial;
    }

    private static TestimonialDto ToDto(Testimonial testimonial) => new(
        testimonial.Id, testimonial.CustomerName, testimonial.CustomerTitle, testimonial.Quote,
        testimonial.Rating, testimonial.ImageUrl, testimonial.DisplayOrder);

    // ---- Coupons -----------------------------------------------------------

    public async Task<PagedResponse<CouponDto>> ListCouponsAsync(
        PageRequest request, CancellationToken cancellationToken = default)
    {
        var page = await _coupons.PageAsync(request.Page, request.PageSize, null, cancellationToken);
        return PagedResponse<CouponDto>.From(page, ToDto);
    }

    public async Task<CouponDto> CreateCouponAsync(
        CouponRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();

        if (await _coupons.GetByCodeAsync(code, cancellationToken) is not null)
        {
            throw new ConflictException($"A coupon with code '{code}' already exists.");
        }

        return ToDto(await _coupons.AddAsync(Apply(new Coupon(), request, code), cancellationToken));
    }

    public async Task<CouponDto> UpdateCouponAsync(
        string id, CouponRequest request, CancellationToken cancellationToken = default)
    {
        var coupon = await _coupons.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Coupon", id);

        var code = request.Code.Trim().ToUpperInvariant();
        var existing = await _coupons.GetByCodeAsync(code, cancellationToken);

        if (existing is not null && existing.Id != id)
        {
            throw new ConflictException($"A coupon with code '{code}' already exists.");
        }

        Apply(coupon, request, code);
        await _coupons.UpdateAsync(coupon, cancellationToken);

        return ToDto(coupon);
    }

    public async Task DeleteCouponAsync(string id, CancellationToken cancellationToken = default)
    {
        _ = await _coupons.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Coupon", id);
        await _coupons.SoftDeleteAsync(id, cancellationToken);
    }

    private static Coupon Apply(Coupon coupon, CouponRequest request, string code)
    {
        coupon.Code = code;
        coupon.Type = request.Type;
        coupon.Value = request.Value;
        coupon.MinOrderTotal = request.MinOrderTotal;
        coupon.MaxDiscount = request.MaxDiscount;
        coupon.UsageLimit = request.UsageLimit;
        coupon.StartsAt = request.StartsAt;
        coupon.ExpiresAt = request.ExpiresAt;
        coupon.IsActive = request.IsActive;
        // UsedCount is never taken from the request — it is owned by checkout.
        return coupon;
    }

    private static CouponDto ToDto(Coupon coupon) => new(
        coupon.Id, coupon.Code, coupon.Type, coupon.Value, coupon.MinOrderTotal,
        coupon.MaxDiscount, coupon.UsageLimit, coupon.UsedCount,
        coupon.StartsAt, coupon.ExpiresAt, coupon.IsActive);

    private static string Normalise(string slug) => slug.Trim().ToLowerInvariant();
}
