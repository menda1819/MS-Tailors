using MSTailors.Application.Carts.Dtos;
using MSTailors.Application.Catalog;
using MSTailors.Application.Common.Exceptions;
using MSTailors.Domain.Entities;
using MSTailors.Domain.ValueObjects;

namespace MSTailors.Application.Carts;

/// <summary>Identifies the cart owner: a signed-in customer or an anonymous cookie id.</summary>
public sealed record CartOwner(string? CustomerId, string? AnonymousId)
{
    public bool IsAnonymous => CustomerId is null;

    public void EnsureValid()
    {
        if (CustomerId is null && string.IsNullOrWhiteSpace(AnonymousId))
        {
            throw new BusinessRuleException("No cart identity was supplied.");
        }
    }
}

public interface ICartService
{
    Task<CartDto> GetAsync(CartOwner owner, CancellationToken cancellationToken = default);
    Task<CartDto> AddItemAsync(CartOwner owner, AddCartItemRequest request, CancellationToken cancellationToken = default);
    Task<CartDto> UpdateItemAsync(CartOwner owner, string sku, UpdateCartItemRequest request, CancellationToken cancellationToken = default);
    Task<CartDto> RemoveItemAsync(CartOwner owner, string sku, CancellationToken cancellationToken = default);
    Task<CartDto> ClearAsync(CartOwner owner, CancellationToken cancellationToken = default);
    Task<CartDto> ApplyCouponAsync(CartOwner owner, ApplyCouponRequest request, CancellationToken cancellationToken = default);
    Task<CartDto> MergeOnLoginAsync(string anonymousId, string customerId, CancellationToken cancellationToken = default);
}

public sealed class CartService : ICartService
{
    private readonly ICartRepository _carts;
    private readonly IProductRepository _products;
    private readonly ICouponRepository _coupons;

    public CartService(ICartRepository carts, IProductRepository products, ICouponRepository coupons)
    {
        _carts = carts;
        _products = products;
        _coupons = coupons;
    }

    public async Task<CartDto> GetAsync(CartOwner owner, CancellationToken cancellationToken = default)
    {
        owner.EnsureValid();
        var cart = await FindAsync(owner, cancellationToken);

        return cart is null
            ? EmptyCart()
            : await ProjectAsync(cart, cancellationToken);
    }

    public async Task<CartDto> AddItemAsync(
        CartOwner owner, AddCartItemRequest request, CancellationToken cancellationToken = default)
    {
        owner.EnsureValid();

        var (product, variant) = await ResolveVariantAsync(request.VariantSku, cancellationToken);

        var cart = await FindAsync(owner, cancellationToken) ?? await CreateAsync(owner, cancellationToken);

        var alreadyInCart = cart.Items
            .FirstOrDefault(item => item.VariantSku == request.VariantSku)?.Quantity ?? 0;

        // Check the total the customer would end up with, not just this addition.
        if (alreadyInCart + request.Quantity > variant.StockQuantity)
        {
            throw new BusinessRuleException(
                $"Only {variant.StockQuantity} of '{product.Name}' ({variant.Size}, {variant.Color}) " +
                $"are available, and your basket already holds {alreadyInCart}.");
        }

        var updated = await _carts.AddItemAsync(cart.Id, new CartItem
        {
            ProductId = product.Id,
            VariantSku = variant.Sku,
            Quantity = request.Quantity,
            UnitPriceSnapshot = product.PriceFor(variant)
        }, cancellationToken);

        return await ProjectAsync(updated, cancellationToken);
    }

    public async Task<CartDto> UpdateItemAsync(
        CartOwner owner, string sku, UpdateCartItemRequest request, CancellationToken cancellationToken = default)
    {
        owner.EnsureValid();

        var cart = await FindAsync(owner, cancellationToken)
            ?? throw new NotFoundException("Cart", "current");

        if (request.Quantity > 0)
        {
            var (product, variant) = await ResolveVariantAsync(sku, cancellationToken);

            if (request.Quantity > variant.StockQuantity)
            {
                throw new BusinessRuleException(
                    $"Only {variant.StockQuantity} of '{product.Name}' ({variant.Size}, {variant.Color}) are available.");
            }
        }

        var updated = await _carts.SetItemQuantityAsync(cart.Id, sku, request.Quantity, cancellationToken)
            ?? throw new NotFoundException("Cart item", sku);

        return await ProjectAsync(updated, cancellationToken);
    }

    public async Task<CartDto> RemoveItemAsync(
        CartOwner owner, string sku, CancellationToken cancellationToken = default)
    {
        owner.EnsureValid();

        var cart = await FindAsync(owner, cancellationToken)
            ?? throw new NotFoundException("Cart", "current");

        var updated = await _carts.RemoveItemAsync(cart.Id, sku, cancellationToken)
            ?? throw new NotFoundException("Cart item", sku);

        return await ProjectAsync(updated, cancellationToken);
    }

    public async Task<CartDto> ClearAsync(CartOwner owner, CancellationToken cancellationToken = default)
    {
        owner.EnsureValid();
        var cart = await FindAsync(owner, cancellationToken);

        if (cart is null)
        {
            return EmptyCart();
        }

        await _carts.ClearAsync(cart.Id, cancellationToken);

        var reloaded = await _carts.GetByIdAsync(cart.Id, cancellationToken);
        return reloaded is null ? EmptyCart() : await ProjectAsync(reloaded, cancellationToken);
    }

    public async Task<CartDto> ApplyCouponAsync(
        CartOwner owner, ApplyCouponRequest request, CancellationToken cancellationToken = default)
    {
        owner.EnsureValid();

        var cart = await FindAsync(owner, cancellationToken)
            ?? throw new NotFoundException("Cart", "current");

        var code = request.Code.Trim().ToUpperInvariant();

        var coupon = await _coupons.GetByCodeAsync(code, cancellationToken)
            ?? throw new BusinessRuleException($"Coupon '{code}' was not recognised.");

        if (!coupon.IsRedeemableAt(DateTime.UtcNow))
        {
            throw new BusinessRuleException($"Coupon '{code}' is no longer valid.");
        }

        cart.CouponCode = code;
        await _carts.UpdateAsync(cart, cancellationToken);

        return await ProjectAsync(cart, cancellationToken);
    }

    public async Task<CartDto> MergeOnLoginAsync(
        string anonymousId, string customerId, CancellationToken cancellationToken = default)
    {
        var merged = await _carts.MergeAnonymousIntoCustomerAsync(anonymousId, customerId, cancellationToken);

        return merged is null ? EmptyCart() : await ProjectAsync(merged, cancellationToken);
    }

    /// <summary>
    /// Rebuilds the cart view from current product data. Lines whose product or
    /// variant has since been withdrawn are dropped with a warning rather than
    /// silently priced at whatever was stored.
    /// </summary>
    private async Task<CartDto> ProjectAsync(Cart cart, CancellationToken cancellationToken)
    {
        var products = await _products.GetByIdsAsync(
            cart.Items.Select(item => item.ProductId), cancellationToken);

        var byId = products.ToDictionary(product => product.Id);

        var priceable = new List<(Product, ProductVariant, int)>();
        var lines = new List<CartLineDto>();
        var warnings = new List<string>();

        foreach (var item in cart.Items)
        {
            if (!byId.TryGetValue(item.ProductId, out var product) || !product.IsActive)
            {
                warnings.Add("An item in your basket is no longer available and was removed.");
                continue;
            }

            var variant = product.Variants.FirstOrDefault(v => v.Sku == item.VariantSku);

            if (variant is null || !variant.IsActive)
            {
                warnings.Add($"'{product.Name}' is no longer available in that size or colour.");
                continue;
            }

            var unitPrice = product.PriceFor(variant);
            priceable.Add((product, variant, item.Quantity));

            if (variant.StockQuantity < item.Quantity)
            {
                warnings.Add(
                    $"Only {variant.StockQuantity} of '{product.Name}' ({variant.Size}) remain.");
            }

            lines.Add(new CartLineDto(
                product.Id, product.Name, product.Slug, variant.Sku, variant.Size, variant.Color,
                variant.ImageUrl ?? product.Images.OrderBy(image => image.Order).FirstOrDefault()?.Url,
                item.Quantity, unitPrice, unitPrice * item.Quantity,
                variant.StockQuantity, variant.InStock,
                // Flag rather than honour the stored snapshot — the customer is
                // told the price moved, and is charged the current one.
                item.UnitPriceSnapshot != unitPrice));
        }

        var coupon = cart.CouponCode is null
            ? null
            : await _coupons.GetByCodeAsync(cart.CouponCode, cancellationToken);

        var pricing = PricingCalculator.Calculate(priceable, coupon, DateTime.UtcNow);
        warnings.AddRange(pricing.Warnings);

        return new CartDto(
            cart.Id, lines, pricing.AppliedCouponCode,
            pricing.Subtotal, pricing.DiscountAmount, pricing.DeliveryFee, pricing.Total,
            "LKR", lines.Sum(line => line.Quantity), warnings);
    }

    private async Task<(Product Product, ProductVariant Variant)> ResolveVariantAsync(
        string sku, CancellationToken cancellationToken)
    {
        var product = await _products.GetByVariantSkuAsync(sku, cancellationToken)
            ?? throw new NotFoundException("Product variant", sku);

        if (!product.IsActive)
        {
            throw new BusinessRuleException("That product is no longer available.");
        }

        var variant = product.Variants.FirstOrDefault(v => v.Sku == sku)
            ?? throw new NotFoundException("Product variant", sku);

        if (!variant.IsActive)
        {
            throw new BusinessRuleException("That size or colour is no longer available.");
        }

        return (product, variant);
    }

    private async Task<Cart?> FindAsync(CartOwner owner, CancellationToken cancellationToken) =>
        owner.CustomerId is not null
            ? await _carts.GetByCustomerIdAsync(owner.CustomerId, cancellationToken)
            : await _carts.GetByAnonymousIdAsync(owner.AnonymousId!, cancellationToken);

    private async Task<Cart> CreateAsync(CartOwner owner, CancellationToken cancellationToken) =>
        await _carts.AddAsync(new Cart
        {
            CustomerId = owner.CustomerId,
            AnonymousId = owner.CustomerId is null ? owner.AnonymousId : null,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        }, cancellationToken);

    private static CartDto EmptyCart() =>
        new(string.Empty, [], null, 0m, 0m, 0m, 0m, "LKR", 0, []);
}
