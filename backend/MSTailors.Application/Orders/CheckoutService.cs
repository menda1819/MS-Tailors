using Microsoft.Extensions.Logging;
using MSTailors.Application.Carts;
using MSTailors.Application.Catalog;
using MSTailors.Application.Common.Dtos;
using MSTailors.Application.Common.Exceptions;
using MSTailors.Application.Orders.Dtos;
using MSTailors.Domain.Entities;
using MSTailors.Domain.Enums;
using MSTailors.Domain.ValueObjects;

namespace MSTailors.Application.Orders;

public interface ICheckoutService
{
    Task<OrderDto> PlaceOrderAsync(
        CartOwner owner, CreateOrderRequest request, CancellationToken cancellationToken = default);

    Task<OrderTrackingDto> TrackAsync(
        string orderNumber, string email, CancellationToken cancellationToken = default);
}

public sealed class CheckoutService : ICheckoutService
{
    private readonly ICartRepository _carts;
    private readonly IProductRepository _products;
    private readonly ICouponRepository _coupons;
    private readonly IOrderRepository _orders;
    private readonly IOrderTransactionStore _transactions;
    private readonly ILogger<CheckoutService> _logger;

    public CheckoutService(
        ICartRepository carts,
        IProductRepository products,
        ICouponRepository coupons,
        IOrderRepository orders,
        IOrderTransactionStore transactions,
        ILogger<CheckoutService> logger)
    {
        _carts = carts;
        _products = products;
        _coupons = coupons;
        _orders = orders;
        _transactions = transactions;
        _logger = logger;
    }

    public async Task<OrderDto> PlaceOrderAsync(
        CartOwner owner, CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var requestedLines = await ResolveRequestedLinesAsync(owner, request, cancellationToken);

        if (requestedLines.Count == 0)
        {
            throw new BusinessRuleException("Your basket is empty.");
        }

        // Load every product fresh. Nothing about price or availability is taken
        // from the cart document, let alone from the request body.
        var products = await _products.GetByIdsAsync(
            requestedLines.Select(line => line.ProductId).Distinct(), cancellationToken);
        var byId = products.ToDictionary(product => product.Id);

        var priceable = new List<(Product, ProductVariant, int)>();
        var reservations = new List<StockReservation>();
        var problems = new List<string>();

        foreach (var line in requestedLines)
        {
            if (!byId.TryGetValue(line.ProductId, out var product) || !product.IsActive)
            {
                problems.Add("An item in your basket is no longer available.");
                continue;
            }

            var variant = product.Variants.FirstOrDefault(v => v.Sku == line.VariantSku);

            if (variant is null || !variant.IsActive)
            {
                problems.Add($"'{product.Name}' is no longer available in that size or colour.");
                continue;
            }

            if (line.Quantity <= 0)
            {
                problems.Add($"'{product.Name}' has an invalid quantity.");
                continue;
            }

            // A first check for a good error message. It is NOT what prevents
            // overselling — the conditional update inside the transaction is.
            if (variant.StockQuantity < line.Quantity)
            {
                problems.Add(
                    $"'{product.Name}' ({variant.Size}, {variant.Color}) has only " +
                    $"{variant.StockQuantity} left; you asked for {line.Quantity}.");
                continue;
            }

            priceable.Add((product, variant, line.Quantity));
            reservations.Add(new StockReservation(product.Id, variant.Sku, line.Quantity));
        }

        if (problems.Count > 0)
        {
            throw new BusinessRuleException("Your order could not be placed.", problems);
        }

        var coupon = await ResolveCouponAsync(owner, request, cancellationToken);
        var pricing = PricingCalculator.Calculate(priceable, coupon, DateTime.UtcNow);

        // A coupon rejected during pricing must not be recorded on the order.
        var appliedCoupon = pricing.AppliedCouponCode is null ? null : coupon;

        var order = BuildOrder(owner, request, pricing, appliedCoupon);

        var outcome = await _transactions.PlaceOrderAsync(
            order, reservations, appliedCoupon?.Id, cancellationToken);

        if (!outcome.Succeeded || outcome.Order is null)
        {
            // Lost the race for the last unit between the check above and the
            // transaction. The transaction rolled everything back.
            throw new BusinessRuleException(
                outcome.FailureReason ?? "Your order could not be placed. Please try again.");
        }

        await ClearCartAsync(owner, cancellationToken);

        _logger.LogInformation("Order {OrderNumber} placed for {Email}",
            outcome.Order.OrderNumber, order.Contact.Email);

        return outcome.Order.ToDto();
    }

    public async Task<OrderTrackingDto> TrackAsync(
        string orderNumber, string email, CancellationToken cancellationToken = default)
    {
        // Both must match. An order number alone is sequential and guessable.
        var order = await _orders.GetForGuestAsync(orderNumber, email, cancellationToken)
            ?? throw new NotFoundException(
                "No order was found with that order number and email address.");

        return new OrderTrackingDto(
            order.OrderNumber,
            order.OrderStatus,
            order.PaymentStatus,
            order.TrackingNumber,
            order.CreatedAt,
            [.. order.StatusHistory.Select(entry =>
                new OrderStatusChangeDto(entry.Status, entry.ChangedAt, entry.ChangedBy, entry.Note))],
            order.Items.Sum(item => item.Quantity),
            order.Total,
            "LKR");
    }

    private async Task<List<(string ProductId, string VariantSku, int Quantity)>> ResolveRequestedLinesAsync(
        CartOwner owner, CreateOrderRequest request, CancellationToken cancellationToken)
    {
        // Explicit items win, but only their quantities — the SKU still has to
        // resolve to a real product, and pricing is recomputed regardless.
        if (request.Items is { Count: > 0 })
        {
            var lines = new List<(string, string, int)>();

            foreach (var item in request.Items)
            {
                var product = await _products.GetByVariantSkuAsync(item.VariantSku, cancellationToken)
                    ?? throw new NotFoundException("Product variant", item.VariantSku);

                lines.Add((product.Id, item.VariantSku, item.Quantity));
            }

            return lines;
        }

        var cart = await FindCartAsync(owner, cancellationToken);

        return cart is null
            ? []
            : [.. cart.Items.Select(item => (item.ProductId, item.VariantSku, item.Quantity))];
    }

    private async Task<Coupon?> ResolveCouponAsync(
        CartOwner owner, CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var code = request.CouponCode;

        if (string.IsNullOrWhiteSpace(code))
        {
            var cart = await FindCartAsync(owner, cancellationToken);
            code = cart?.CouponCode;
        }

        return string.IsNullOrWhiteSpace(code)
            ? null
            : await _coupons.GetByCodeAsync(code, cancellationToken);
    }

    private static Order BuildOrder(
        CartOwner owner, CreateOrderRequest request, PricingResult pricing, Coupon? coupon)
    {
        var order = new Order
        {
            CustomerId = owner.CustomerId,
            Contact = new ContactDetails
            {
                FullName = request.Contact.FullName.Trim(),
                Email = request.Contact.Email.Trim().ToLowerInvariant(),
                Phone = request.Contact.Phone.Trim()
            },
            ShippingAddress = new ShippingAddress
            {
                Line1 = request.ShippingAddress.Line1.Trim(),
                Line2 = string.IsNullOrWhiteSpace(request.ShippingAddress.Line2)
                    ? null
                    : request.ShippingAddress.Line2.Trim(),
                City = request.ShippingAddress.City.Trim(),
                PostalCode = string.IsNullOrWhiteSpace(request.ShippingAddress.PostalCode)
                    ? null
                    : request.ShippingAddress.PostalCode.Trim()
            },
            SpecialInstructions = string.IsNullOrWhiteSpace(request.SpecialInstructions)
                ? null
                : request.SpecialInstructions.Trim(),
            Subtotal = pricing.Subtotal,
            DiscountAmount = pricing.DiscountAmount,
            CouponCode = coupon?.Code,
            DeliveryFee = pricing.DeliveryFee,
            Total = pricing.Total,
            PaymentMethod = request.PaymentMethod,
            // Cash on delivery is confirmed immediately; anything that needs a
            // gateway stays pending until the provider says otherwise.
            PaymentStatus = PaymentStatus.Pending,
            OrderStatus = request.PaymentMethod == PaymentMethod.CashOnDelivery
                ? OrderStatus.Confirmed
                : OrderStatus.New,
            Items =
            [
                .. pricing.Lines.Select(line => new OrderItem
                {
                    ProductId = line.Product.Id,
                    BrandId = line.Product.BrandId,
                    ProductNameSnapshot = line.Product.Name,
                    VariantSku = line.Variant.Sku,
                    Size = line.Variant.Size,
                    Color = line.Variant.Color,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    LineTotal = line.LineTotal
                })
            ]
        };

        order.StatusHistory =
        [
            new OrderStatusChange
            {
                Status = order.OrderStatus,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = "system",
                Note = "Order placed."
            }
        ];

        return order;
    }

    private async Task<Cart?> FindCartAsync(CartOwner owner, CancellationToken cancellationToken) =>
        owner.CustomerId is not null
            ? await _carts.GetByCustomerIdAsync(owner.CustomerId, cancellationToken)
            : string.IsNullOrWhiteSpace(owner.AnonymousId)
                ? null
                : await _carts.GetByAnonymousIdAsync(owner.AnonymousId, cancellationToken);

    private async Task ClearCartAsync(CartOwner owner, CancellationToken cancellationToken)
    {
        var cart = await FindCartAsync(owner, cancellationToken);

        if (cart is not null)
        {
            await _carts.ClearAsync(cart.Id, cancellationToken);
        }
    }
}
