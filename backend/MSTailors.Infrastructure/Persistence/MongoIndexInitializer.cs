using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MSTailors.Domain.Entities;

namespace MSTailors.Infrastructure.Persistence;

/// <summary>
/// Creates every index the data model requires.
/// </summary>
/// <remarks>
/// Idempotent: <c>createIndexes</c> is a no-op when an index with the same name
/// and identical options already exists, so this is safe to run on every start.
///
/// Indexes are named explicitly. Relying on generated names makes it impossible
/// to tell an intentional change from an accidental duplicate.
/// </remarks>
public sealed class MongoIndexInitializer
{
    private readonly IMongoContext _context;
    private readonly ILogger<MongoIndexInitializer> _logger;

    public MongoIndexInitializer(IMongoContext context, ILogger<MongoIndexInitializer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task CreateIndexesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Ensuring MongoDB indexes on {Database}", _context.Database.DatabaseNamespace.DatabaseName);

        await CreateBrandIndexesAsync(cancellationToken);
        await CreateCategoryIndexesAsync(cancellationToken);
        await CreateProductIndexesAsync(cancellationToken);
        await CreateCustomerIndexesAsync(cancellationToken);
        await CreateCartIndexesAsync(cancellationToken);
        await CreateOrderIndexesAsync(cancellationToken);
        await CreateCouponIndexesAsync(cancellationToken);
        await CreateServiceIndexesAsync(cancellationToken);
        await CreateInquiryIndexesAsync(cancellationToken);
        await CreateGalleryIndexesAsync(cancellationToken);
        await CreateTestimonialIndexesAsync(cancellationToken);
        await CreateBlogPostIndexesAsync(cancellationToken);
        await CreateSizeGuideIndexesAsync(cancellationToken);

        _logger.LogInformation("MongoDB indexes are up to date");
    }

    private async Task CreateBrandIndexesAsync(CancellationToken cancellationToken)
    {
        var keys = Builders<Brand>.IndexKeys;
        await CreateAsync(_context.Brands,
        [
            new CreateIndexModel<Brand>(keys.Ascending(brand => brand.Slug),
                new CreateIndexOptions { Name = "ux_brands_slug", Unique = true }),
            new CreateIndexModel<Brand>(
                keys.Ascending(brand => brand.IsActive).Ascending(brand => brand.DisplayOrder),
                new CreateIndexOptions { Name = "ix_brands_active_order" })
        ], cancellationToken);
    }

    private async Task CreateCategoryIndexesAsync(CancellationToken cancellationToken)
    {
        var keys = Builders<Category>.IndexKeys;
        await CreateAsync(_context.Categories,
        [
            new CreateIndexModel<Category>(keys.Ascending(category => category.Slug),
                new CreateIndexOptions { Name = "ux_categories_slug", Unique = true }),
            new CreateIndexModel<Category>(
                keys.Ascending(category => category.ParentCategoryId).Ascending(category => category.DisplayOrder),
                new CreateIndexOptions { Name = "ix_categories_parent_order" })
        ], cancellationToken);
    }

    private async Task CreateProductIndexesAsync(CancellationToken cancellationToken)
    {
        var keys = Builders<Product>.IndexKeys;
        await CreateAsync(_context.Products,
        [
            new CreateIndexModel<Product>(keys.Ascending(product => product.Slug),
                new CreateIndexOptions { Name = "ux_products_slug", Unique = true }),

            // Unique across every product, not per product. A sparse index would
            // let two products with no variants collide on missing keys, so the
            // partial filter restricts uniqueness to documents that have variants.
            new CreateIndexModel<Product>(keys.Ascending("variants.sku"),
                new CreateIndexOptions<Product>
                {
                    Name = "ux_products_variant_sku",
                    Unique = true,
                    PartialFilterExpression = Builders<Product>.Filter.Exists("variants.0")
                }),

            new CreateIndexModel<Product>(
                keys.Ascending(product => product.BrandId).Ascending(product => product.IsActive),
                new CreateIndexOptions { Name = "ix_products_brand_active" }),

            new CreateIndexModel<Product>(
                keys.Ascending(product => product.CategoryIds).Ascending(product => product.IsActive),
                new CreateIndexOptions { Name = "ix_products_categories_active" }),

            new CreateIndexModel<Product>(
                keys.Ascending(product => product.IsFeatured).Ascending(product => product.IsActive),
                new CreateIndexOptions { Name = "ix_products_featured_active" }),

            new CreateIndexModel<Product>(keys.Ascending(product => product.BasePrice),
                new CreateIndexOptions { Name = "ix_products_baseprice" }),

            // One text index per collection is a MongoDB hard limit. Weights bias
            // a name match above a description match.
            new CreateIndexModel<Product>(
                keys.Text(product => product.Name)
                    .Text(product => product.Description)
                    .Text(product => product.Tags),
                new CreateIndexOptions
                {
                    Name = "tx_products_search",
                    Weights = new BsonDocument { { "name", 10 }, { "tags", 5 }, { "description", 1 } },
                    DefaultLanguage = "english"
                })
        ], cancellationToken);
    }

    private async Task CreateCustomerIndexesAsync(CancellationToken cancellationToken)
    {
        var keys = Builders<Customer>.IndexKeys;
        await CreateAsync(_context.Customers,
        [
            new CreateIndexModel<Customer>(keys.Ascending(customer => customer.Email),
                new CreateIndexOptions { Name = "ux_customers_email", Unique = true }),
            new CreateIndexModel<Customer>(keys.Ascending("refreshTokens.token"),
                new CreateIndexOptions { Name = "ix_customers_refresh_token" })
        ], cancellationToken);
    }

    private async Task CreateCartIndexesAsync(CancellationToken cancellationToken)
    {
        var keys = Builders<Cart>.IndexKeys;
        await CreateAsync(_context.Carts,
        [
            // Sparse: a cart has either a customerId or an anonymousId, never both,
            // so the absent key must not be indexed as null across every document.
            new CreateIndexModel<Cart>(keys.Ascending(cart => cart.CustomerId),
                new CreateIndexOptions { Name = "ix_carts_customer", Sparse = true }),
            new CreateIndexModel<Cart>(keys.Ascending(cart => cart.AnonymousId),
                new CreateIndexOptions { Name = "ix_carts_anonymous", Sparse = true }),

            // TTL: MongoDB deletes the document once expiresAt passes.
            // ExpireAfter of zero means "expire exactly at the stored instant".
            new CreateIndexModel<Cart>(keys.Ascending(cart => cart.ExpiresAt),
                new CreateIndexOptions { Name = "ttl_carts_expiresat", ExpireAfter = TimeSpan.Zero })
        ], cancellationToken);
    }

    private async Task CreateOrderIndexesAsync(CancellationToken cancellationToken)
    {
        var keys = Builders<Order>.IndexKeys;
        await CreateAsync(_context.Orders,
        [
            new CreateIndexModel<Order>(keys.Ascending(order => order.OrderNumber),
                new CreateIndexOptions { Name = "ux_orders_ordernumber", Unique = true }),
            new CreateIndexModel<Order>(
                keys.Ascending(order => order.CustomerId).Descending(order => order.CreatedAt),
                new CreateIndexOptions { Name = "ix_orders_customer_created" }),
            new CreateIndexModel<Order>(
                keys.Ascending("contact.email").Descending(order => order.CreatedAt),
                new CreateIndexOptions { Name = "ix_orders_email_created" }),
            new CreateIndexModel<Order>(
                keys.Ascending(order => order.OrderStatus).Descending(order => order.CreatedAt),
                new CreateIndexOptions { Name = "ix_orders_status_created" }),
            new CreateIndexModel<Order>(keys.Ascending(order => order.PaymentStatus),
                new CreateIndexOptions { Name = "ix_orders_paymentstatus" })
        ], cancellationToken);
    }

    private async Task CreateCouponIndexesAsync(CancellationToken cancellationToken)
    {
        var keys = Builders<Coupon>.IndexKeys;
        await CreateAsync(_context.Coupons,
        [
            new CreateIndexModel<Coupon>(keys.Ascending(coupon => coupon.Code),
                new CreateIndexOptions { Name = "ux_coupons_code", Unique = true }),
            new CreateIndexModel<Coupon>(
                keys.Ascending(coupon => coupon.IsActive).Ascending(coupon => coupon.ExpiresAt),
                new CreateIndexOptions { Name = "ix_coupons_active_expiry" })
        ], cancellationToken);
    }

    private async Task CreateServiceIndexesAsync(CancellationToken cancellationToken)
    {
        var keys = Builders<Service>.IndexKeys;
        await CreateAsync(_context.Services,
        [
            new CreateIndexModel<Service>(keys.Ascending(service => service.Slug),
                new CreateIndexOptions { Name = "ux_services_slug", Unique = true }),
            new CreateIndexModel<Service>(
                keys.Ascending(service => service.IsActive).Ascending(service => service.DisplayOrder),
                new CreateIndexOptions { Name = "ix_services_active_order" })
        ], cancellationToken);
    }

    private async Task CreateInquiryIndexesAsync(CancellationToken cancellationToken)
    {
        var keys = Builders<Inquiry>.IndexKeys;
        await CreateAsync(_context.Inquiries,
        [
            new CreateIndexModel<Inquiry>(
                keys.Ascending(inquiry => inquiry.Status).Descending(inquiry => inquiry.CreatedAt),
                new CreateIndexOptions { Name = "ix_inquiries_status_created" }),
            new CreateIndexModel<Inquiry>(keys.Descending(inquiry => inquiry.CreatedAt),
                new CreateIndexOptions { Name = "ix_inquiries_created" }),
            new CreateIndexModel<Inquiry>(keys.Ascending(inquiry => inquiry.Email),
                new CreateIndexOptions { Name = "ix_inquiries_email" })
        ], cancellationToken);
    }

    private async Task CreateGalleryIndexesAsync(CancellationToken cancellationToken)
    {
        var keys = Builders<GalleryItem>.IndexKeys;
        await CreateAsync(_context.GalleryItems,
        [
            new CreateIndexModel<GalleryItem>(
                keys.Ascending(item => item.Category)
                    .Ascending(item => item.IsActive)
                    .Ascending(item => item.DisplayOrder),
                new CreateIndexOptions { Name = "ix_gallery_category_active_order" })
        ], cancellationToken);
    }

    private async Task CreateTestimonialIndexesAsync(CancellationToken cancellationToken)
    {
        var keys = Builders<Testimonial>.IndexKeys;
        await CreateAsync(_context.Testimonials,
        [
            new CreateIndexModel<Testimonial>(
                keys.Ascending(testimonial => testimonial.IsApproved)
                    .Ascending(testimonial => testimonial.IsActive)
                    .Ascending(testimonial => testimonial.DisplayOrder),
                new CreateIndexOptions { Name = "ix_testimonials_approved_active_order" })
        ], cancellationToken);
    }

    private async Task CreateBlogPostIndexesAsync(CancellationToken cancellationToken)
    {
        var keys = Builders<BlogPost>.IndexKeys;
        await CreateAsync(_context.BlogPosts,
        [
            new CreateIndexModel<BlogPost>(keys.Ascending(post => post.Slug),
                new CreateIndexOptions { Name = "ux_blogposts_slug", Unique = true }),
            new CreateIndexModel<BlogPost>(
                keys.Ascending(post => post.IsPublished).Descending(post => post.PublishedAt),
                new CreateIndexOptions { Name = "ix_blogposts_published" })
        ], cancellationToken);
    }

    private async Task CreateSizeGuideIndexesAsync(CancellationToken cancellationToken)
    {
        var keys = Builders<SizeGuide>.IndexKeys;
        await CreateAsync(_context.SizeGuides,
        [
            new CreateIndexModel<SizeGuide>(keys.Ascending(guide => guide.Slug),
                new CreateIndexOptions { Name = "ux_sizeguides_slug", Unique = true })
        ], cancellationToken);
    }

    private async Task CreateAsync<T>(
        IMongoCollection<T> collection,
        IEnumerable<CreateIndexModel<T>> models,
        CancellationToken cancellationToken)
    {
        var list = models.ToList();

        try
        {
            await collection.Indexes.CreateManyAsync(list, cancellationToken);
        }
        catch (MongoCommandException ex) when (ex.CodeName == "IndexOptionsConflict"
                                            || ex.CodeName == "IndexKeySpecsConflict")
        {
            // An index of the same name exists with different options. Recreating
            // it silently could drop a unique constraint, so this is surfaced
            // rather than swallowed.
            _logger.LogError(ex,
                "Index definition conflict on {Collection}. An existing index has the same name but different " +
                "options. Drop it manually and restart to apply the new definition.",
                collection.CollectionNamespace.CollectionName);
            throw;
        }
    }
}
