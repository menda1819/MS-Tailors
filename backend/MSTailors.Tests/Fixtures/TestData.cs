using MSTailors.Domain.Entities;
using MSTailors.Domain.ValueObjects;

namespace MSTailors.Tests.Fixtures;

/// <summary>Minimal, explicit builders. Each test overrides only what it cares about.</summary>
public static class TestData
{
    public static Brand Brand(string slug = "test-brand", string? name = null) => new()
    {
        Name = name ?? slug.Replace('-', ' '),
        Slug = slug,
        Tagline = "Test tagline",
        Story = "Test story",
        Philosophy = "Test philosophy",
        TargetAudience = "Test audience",
        LogoUrl = "/logo.svg",
        HeroImageUrl = "/hero.jpg"
    };

    public static Product Product(
        string slug = "test-product",
        string brandId = "507f1f77bcf86cd799439011",
        decimal basePrice = 10_000m,
        decimal? salePrice = null,
        bool isActive = true,
        params ProductVariant[] variants) => new()
    {
        Name = slug.Replace('-', ' '),
        Slug = slug,
        BrandId = brandId,
        Description = "Test description",
        ShortDescription = "Short description",
        Fabric = "Cotton",
        CareInstructions = "Machine wash",
        BasePrice = basePrice,
        SalePrice = salePrice,
        IsActive = isActive,
        Variants = variants.Length > 0
            ? [.. variants]
            : [Variant()]
    };

    public static ProductVariant Variant(
        string sku = "TEST-SKU-M",
        string size = "M",
        string color = "Navy",
        int stock = 10,
        decimal? priceOverride = null) => new()
    {
        Sku = sku,
        Size = size,
        Color = color,
        ColorHex = "#1F2A44",
        StockQuantity = stock,
        PriceOverride = priceOverride
    };

    public static Customer Customer(string email = "test@example.com") => new()
    {
        Email = email,
        PasswordHash = "hash",
        FullName = "Test Customer",
        Phone = "+94770000000"
    };

    public static Cart Cart(string? customerId = null, string? anonymousId = null) => new()
    {
        CustomerId = customerId,
        AnonymousId = anonymousId,
        ExpiresAt = DateTime.UtcNow.AddDays(30)
    };

    public static CartItem CartItem(string sku = "TEST-SKU-M", int quantity = 1, decimal price = 10_000m) => new()
    {
        ProductId = "507f1f77bcf86cd799439011",
        VariantSku = sku,
        Quantity = quantity,
        UnitPriceSnapshot = price
    };

    public static Order Order(
        string orderNumber = "MST-2026-0001",
        string email = "buyer@example.com",
        string? customerId = null) => new()
    {
        OrderNumber = orderNumber,
        CustomerId = customerId,
        Contact = new ContactDetails { FullName = "Test Buyer", Email = email, Phone = "+94770000000" },
        ShippingAddress = new ShippingAddress { Line1 = "28 Station Road", City = "Panadura" },
        Items =
        [
            new OrderItem
            {
                ProductId = "507f1f77bcf86cd799439011",
                BrandId = "507f1f77bcf86cd799439012",
                ProductNameSnapshot = "Test Product",
                VariantSku = "TEST-SKU-M",
                Size = "M",
                Color = "Navy",
                Quantity = 1,
                UnitPrice = 10_000m,
                LineTotal = 10_000m
            }
        ],
        Subtotal = 10_000m,
        DeliveryFee = 500m,
        Total = 10_500m
    };
}
