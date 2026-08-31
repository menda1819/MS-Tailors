using MSTailors.Domain.Enums;

namespace MSTailors.Application.Orders.Dtos;

public sealed class CreateOrderRequest
{
    public ContactRequest Contact { get; init; } = new();
    public ShippingAddressRequest ShippingAddress { get; init; } = new();
    public string? SpecialInstructions { get; init; }
    public PaymentMethod PaymentMethod { get; init; } = PaymentMethod.CashOnDelivery;
    public string? CouponCode { get; init; }

    /// <summary>
    /// Optional. When omitted the server uses the caller's server-side cart.
    /// Quantities are honoured; any price sent by the client is ignored.
    /// </summary>
    public IReadOnlyList<OrderLineRequest>? Items { get; init; }
}

public sealed class OrderLineRequest
{
    public string VariantSku { get; init; } = string.Empty;
    public int Quantity { get; init; }
}

public sealed class ContactRequest
{
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
}

public sealed class ShippingAddressRequest
{
    public string Line1 { get; init; } = string.Empty;
    public string? Line2 { get; init; }
    public string City { get; init; } = string.Empty;
    public string? PostalCode { get; init; }
}

public sealed record OrderItemDto(
    string ProductId, string BrandId, string ProductName, string VariantSku,
    string Size, string Color, int Quantity, decimal UnitPrice, decimal LineTotal);

public sealed record OrderStatusChangeDto(OrderStatus Status, DateTime ChangedAt, string ChangedBy, string? Note);

public sealed record OrderDto(
    string Id,
    string OrderNumber,
    string? CustomerId,
    ContactDto Contact,
    ShippingAddressDto ShippingAddress,
    string? SpecialInstructions,
    IReadOnlyList<OrderItemDto> Items,
    decimal Subtotal,
    decimal DiscountAmount,
    string? CouponCode,
    decimal DeliveryFee,
    decimal Total,
    string Currency,
    PaymentMethod PaymentMethod,
    PaymentStatus PaymentStatus,
    OrderStatus OrderStatus,
    IReadOnlyList<OrderStatusChangeDto> StatusHistory,
    string? TrackingNumber,
    DateTime PlacedAt);

public sealed record ContactDto(string FullName, string Email, string Phone);

public sealed record ShippingAddressDto(string Line1, string? Line2, string City, string? PostalCode);

/// <summary>Compact shape for order lists.</summary>
public sealed record OrderSummaryDto(
    string Id, string OrderNumber, decimal Total, string Currency,
    PaymentStatus PaymentStatus, OrderStatus OrderStatus, int ItemCount, DateTime PlacedAt);

/// <summary>
/// Public tracking view. Deliberately omits the address, phone, and line
/// pricing — an order number plus email should not expose the full record.
/// </summary>
public sealed record OrderTrackingDto(
    string OrderNumber,
    OrderStatus OrderStatus,
    PaymentStatus PaymentStatus,
    string? TrackingNumber,
    DateTime PlacedAt,
    IReadOnlyList<OrderStatusChangeDto> StatusHistory,
    int ItemCount,
    decimal Total,
    string Currency);
