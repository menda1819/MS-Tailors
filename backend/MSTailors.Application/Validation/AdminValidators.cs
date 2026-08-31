using FluentValidation;
using MSTailors.Application.Admin;
using MSTailors.Application.Admin.Dtos;

namespace MSTailors.Application.Validation;

public sealed class BrandRequestValidator : AbstractValidator<BrandRequest>
{
    public BrandRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(120);
        RuleFor(request => request.Slug).Slug();
        RuleFor(request => request.Tagline).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Story).NotEmpty().MaximumLength(8000);
        RuleFor(request => request.Philosophy).MaximumLength(2000);
        RuleFor(request => request.TargetAudience).MaximumLength(500);
        RuleFor(request => request.LogoUrl).NotEmpty().MaximumLength(500);
        RuleFor(request => request.HeroImageUrl).NotEmpty().MaximumLength(500);
        RuleFor(request => request.DisplayOrder).GreaterThanOrEqualTo(0);
        RuleFor(request => request.SeoTitle).MaximumLength(70)
            .WithMessage("Search engines truncate titles beyond about 70 characters.");
        RuleFor(request => request.SeoDescription).MaximumLength(160)
            .WithMessage("Search engines truncate descriptions beyond about 160 characters.");
    }
}

public sealed class CategoryRequestValidator : AbstractValidator<CategoryRequest>
{
    public CategoryRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(100);
        RuleFor(request => request.Slug).Slug();
        RuleFor(request => request.ParentCategoryId).ObjectId()
            .When(request => !string.IsNullOrWhiteSpace(request.ParentCategoryId));
        RuleFor(request => request.ImageUrl).MaximumLength(500);
        RuleFor(request => request.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class SpecificationRequestValidator : AbstractValidator<SpecificationRequest>
{
    public SpecificationRequestValidator()
    {
        RuleFor(specification => specification.Label).NotEmpty().MaximumLength(60);
        RuleFor(specification => specification.Value).NotEmpty().MaximumLength(200);
    }
}

public sealed class ProductImageRequestValidator : AbstractValidator<ProductImageRequest>
{
    public ProductImageRequestValidator()
    {
        RuleFor(image => image.Url).NotEmpty().MaximumLength(500);
        // Alt text is required. An empty string is allowed only for images that
        // are genuinely decorative, which a product photo never is.
        RuleFor(image => image.Alt).NotEmpty().MaximumLength(200)
            .WithMessage("Alt text is required for accessibility.");
        RuleFor(image => image.Order).GreaterThanOrEqualTo(0);
    }
}

public sealed class ProductRequestValidator : AbstractValidator<ProductRequest>
{
    public ProductRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Slug).Slug();
        RuleFor(request => request.BrandId).ObjectId();
        RuleForEach(request => request.CategoryIds).ObjectId();
        RuleFor(request => request.Description).NotEmpty().MaximumLength(8000);
        RuleFor(request => request.ShortDescription).NotEmpty().MaximumLength(300);
        RuleFor(request => request.Fabric).MaximumLength(200);
        RuleFor(request => request.CareInstructions).MaximumLength(500);
        RuleFor(request => request.BasePrice).Money().GreaterThan(0)
            .WithMessage("A product needs a price above zero.");

        RuleFor(request => request.SalePrice!.Value).Money()
            .When(request => request.SalePrice.HasValue);

        RuleFor(request => request.SalePrice)
            .LessThan(request => request.BasePrice)
            .When(request => request.SalePrice.HasValue)
            .WithMessage("The sale price must be below the base price.");

        RuleForEach(request => request.Specifications).SetValidator(new SpecificationRequestValidator());
        RuleForEach(request => request.Images).SetValidator(new ProductImageRequestValidator());
        RuleForEach(request => request.Tags).NotEmpty().MaximumLength(40);

        RuleFor(request => request.SizeGuideId).ObjectId()
            .When(request => !string.IsNullOrWhiteSpace(request.SizeGuideId));

        RuleFor(request => request.SeoTitle).MaximumLength(70);
        RuleFor(request => request.SeoDescription).MaximumLength(160);
    }
}

public sealed class VariantRequestValidator : AbstractValidator<VariantRequest>
{
    public VariantRequestValidator()
    {
        RuleFor(request => request.Sku)
            .NotEmpty().MaximumLength(64)
            .Matches("^[A-Za-z0-9._-]+$")
            .WithMessage("A SKU may contain only letters, numbers, dots, underscores and hyphens.");

        RuleFor(request => request.Size).NotEmpty().MaximumLength(20);
        RuleFor(request => request.Color).NotEmpty().MaximumLength(40);

        RuleFor(request => request.ColorHex)
            .Must(value => string.IsNullOrWhiteSpace(value) || CommonRules.HexColourPattern().IsMatch(value))
            .WithMessage("Use a hex colour such as #1F2A44.");

        RuleFor(request => request.StockQuantity)
            .InclusiveBetween(0, 1_000_000)
            .WithMessage("Stock must be zero or more.");

        RuleFor(request => request.PriceOverride!.Value).Money()
            .When(request => request.PriceOverride.HasValue);

        RuleFor(request => request.ImageUrl).MaximumLength(500);
    }
}

public sealed class ServiceRequestValidator : AbstractValidator<ServiceRequest>
{
    public ServiceRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(150);
        RuleFor(request => request.Slug).Slug();
        RuleFor(request => request.Category).NotEmpty().MaximumLength(60);
        RuleFor(request => request.ShortDescription).NotEmpty().MaximumLength(300);
        RuleFor(request => request.LongDescription).NotEmpty().MaximumLength(8000);
        RuleFor(request => request.ImageUrl).MaximumLength(500);
        RuleForEach(request => request.BulletPoints).NotEmpty().MaximumLength(300);
        RuleFor(request => request.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class GalleryItemRequestValidator : AbstractValidator<GalleryItemRequest>
{
    public GalleryItemRequestValidator()
    {
        RuleFor(request => request.ImageUrl).NotEmpty().MaximumLength(500);
        RuleFor(request => request.ThumbnailUrl).MaximumLength(500);
        RuleFor(request => request.Caption).MaximumLength(300);
        RuleFor(request => request.AltText).NotEmpty().MaximumLength(200)
            .WithMessage("Alt text is required for accessibility.");
        RuleFor(request => request.Category).IsInEnum();
        RuleFor(request => request.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class BlogPostRequestValidator : AbstractValidator<BlogPostRequest>
{
    public BlogPostRequestValidator()
    {
        RuleFor(request => request.Title).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Slug).Slug();
        RuleFor(request => request.Excerpt).NotEmpty().MaximumLength(400);
        RuleFor(request => request.Content).NotEmpty();
        RuleFor(request => request.CoverImageUrl).MaximumLength(500);
        RuleFor(request => request.Author).NotEmpty().MaximumLength(120);
        RuleForEach(request => request.Tags).NotEmpty().MaximumLength(40);
    }
}

public sealed class TestimonialRequestValidator : AbstractValidator<TestimonialRequest>
{
    public TestimonialRequestValidator()
    {
        RuleFor(request => request.CustomerName).NotEmpty().MaximumLength(120);
        RuleFor(request => request.CustomerTitle).MaximumLength(120);
        RuleFor(request => request.Quote).NotEmpty().MaximumLength(2000);
        RuleFor(request => request.Rating).InclusiveBetween(1, 5);
        RuleFor(request => request.ImageUrl).MaximumLength(500);
        RuleFor(request => request.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class CouponRequestValidator : AbstractValidator<CouponRequest>
{
    public CouponRequestValidator()
    {
        RuleFor(request => request.Code)
            .NotEmpty().MaximumLength(40)
            .Matches("^[A-Za-z0-9-]+$")
            .WithMessage("A coupon code may contain only letters, numbers and hyphens.");

        RuleFor(request => request.Type).IsInEnum();

        RuleFor(request => request.Value)
            .InclusiveBetween(1, 100)
            .When(request => request.Type == Domain.Enums.CouponType.Percentage)
            .WithMessage("A percentage discount must be between 1 and 100.");

        RuleFor(request => request.Value).Money().GreaterThan(0)
            .When(request => request.Type == Domain.Enums.CouponType.FixedAmount);

        RuleFor(request => request.MinOrderTotal).Money();
        RuleFor(request => request.MaxDiscount!.Value).Money().When(request => request.MaxDiscount.HasValue);
        RuleFor(request => request.UsageLimit!.Value).GreaterThan(0).When(request => request.UsageLimit.HasValue);

        RuleFor(request => request.ExpiresAt)
            .GreaterThan(request => request.StartsAt)
            .WithMessage("The expiry must be after the start date.");
    }
}

public sealed class UpdateOrderStatusRequestValidator : AbstractValidator<UpdateOrderStatusRequest>
{
    public UpdateOrderStatusRequestValidator()
    {
        RuleFor(request => request.Status).IsInEnum();
        RuleFor(request => request.Note).MaximumLength(500);
        RuleFor(request => request.TrackingNumber).MaximumLength(100);
    }
}

public sealed class AdminOrderQueryRequestValidator : AbstractValidator<AdminOrderQueryRequest>
{
    public AdminOrderQueryRequestValidator()
    {
        Include(new PageRequestValidator());
        RuleFor(request => request.Status).IsInEnum().When(request => request.Status.HasValue);
        RuleFor(request => request.PaymentStatus).IsInEnum().When(request => request.PaymentStatus.HasValue);
        RuleFor(request => request.Email).MaximumLength(254);
        RuleFor(request => request.To)
            .GreaterThan(request => request.From!.Value)
            .When(request => request.From.HasValue && request.To.HasValue)
            .WithMessage("The end of the range must be after the start.");
    }
}

public sealed class BulkStockLineValidator : AbstractValidator<BulkStockLine>
{
    public BulkStockLineValidator()
    {
        RuleFor(line => line.ProductId).ObjectId();
        RuleFor(line => line.Sku).NotEmpty().MaximumLength(64);
        RuleFor(line => line.StockQuantity).InclusiveBetween(0, 1_000_000);
    }
}

public sealed class BulkStockRequestValidator : AbstractValidator<BulkStockRequest>
{
    public BulkStockRequestValidator()
    {
        RuleFor(request => request.Updates)
            .NotEmpty().WithMessage("Nothing to update.")
            // Bounded so one request cannot rewrite the whole catalogue.
            .Must(updates => updates.Count <= 500)
            .WithMessage("Update at most 500 variants at a time.");

        RuleForEach(request => request.Updates).SetValidator(new BulkStockLineValidator());
    }
}

public sealed class UpdateInquiryStatusRequestValidator : AbstractValidator<UpdateInquiryStatusRequest>
{
    public UpdateInquiryStatusRequestValidator() => RuleFor(request => request.Status).IsInEnum();
}

public sealed class InquiryQueryRequestValidator : AbstractValidator<InquiryQueryRequest>
{
    public InquiryQueryRequestValidator()
    {
        Include(new PageRequestValidator());
        RuleFor(request => request.Status).IsInEnum().When(request => request.Status.HasValue);
        RuleFor(request => request.ServiceNeeded).IsInEnum().When(request => request.ServiceNeeded.HasValue);
    }
}

public sealed class AdminCustomerQueryRequestValidator : AbstractValidator<AdminCustomerQueryRequest>
{
    public AdminCustomerQueryRequestValidator()
    {
        Include(new PageRequestValidator());
        RuleFor(request => request.Search).MaximumLength(120);
        RuleFor(request => request.Role).IsInEnum().When(request => request.Role.HasValue);
    }
}
