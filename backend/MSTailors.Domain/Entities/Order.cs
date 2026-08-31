using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MSTailors.Domain.Common;
using MSTailors.Domain.Enums;
using MSTailors.Domain.ValueObjects;

namespace MSTailors.Domain.Entities;

/// <summary>
/// A placed shop order. Every monetary field is computed server-side from the
/// database at order time and never accepted from the browser.
/// </summary>
public class Order : Entity
{
    /// <summary>Human-readable and unique, e.g. MST-2026-0001.</summary>
    [BsonElement("orderNumber")]
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Null for a guest order.</summary>
    [BsonElement("customerId")]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]
    public string? CustomerId { get; set; }

    [BsonElement("contact")]
    public ContactDetails Contact { get; set; } = new();

    [BsonElement("shippingAddress")]
    public ShippingAddress ShippingAddress { get; set; } = new();

    [BsonElement("specialInstructions")]
    [BsonIgnoreIfNull]
    public string? SpecialInstructions { get; set; }

    [BsonElement("items")]
    public List<OrderItem> Items { get; set; } = [];

    [BsonElement("subtotal")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Subtotal { get; set; }

    [BsonElement("discountAmount")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal DiscountAmount { get; set; }

    [BsonElement("couponCode")]
    [BsonIgnoreIfNull]
    public string? CouponCode { get; set; }

    [BsonElement("deliveryFee")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal DeliveryFee { get; set; }

    [BsonElement("total")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Total { get; set; }

    [BsonElement("paymentMethod")]
    [BsonRepresentation(BsonType.String)]
    public PaymentMethod PaymentMethod { get; set; }

    [BsonElement("paymentStatus")]
    [BsonRepresentation(BsonType.String)]
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    [BsonElement("orderStatus")]
    [BsonRepresentation(BsonType.String)]
    public OrderStatus OrderStatus { get; set; } = OrderStatus.New;

    [BsonElement("statusHistory")]
    public List<OrderStatusChange> StatusHistory { get; set; } = [];

    [BsonElement("trackingNumber")]
    [BsonIgnoreIfNull]
    public string? TrackingNumber { get; set; }
}
