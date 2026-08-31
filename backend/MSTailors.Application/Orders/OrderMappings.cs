using MSTailors.Application.Orders.Dtos;
using MSTailors.Domain.Entities;

namespace MSTailors.Application.Orders;

public static class OrderMappings
{
    public static OrderDto ToDto(this Order order) => new(
        order.Id,
        order.OrderNumber,
        order.CustomerId,
        new ContactDto(order.Contact.FullName, order.Contact.Email, order.Contact.Phone),
        new ShippingAddressDto(order.ShippingAddress.Line1, order.ShippingAddress.Line2,
            order.ShippingAddress.City, order.ShippingAddress.PostalCode),
        order.SpecialInstructions,
        [.. order.Items.Select(item => new OrderItemDto(
            item.ProductId, item.BrandId, item.ProductNameSnapshot, item.VariantSku,
            item.Size, item.Color, item.Quantity, item.UnitPrice, item.LineTotal))],
        order.Subtotal,
        order.DiscountAmount,
        order.CouponCode,
        order.DeliveryFee,
        order.Total,
        "LKR",
        order.PaymentMethod,
        order.PaymentStatus,
        order.OrderStatus,
        [.. order.StatusHistory.Select(entry =>
            new OrderStatusChangeDto(entry.Status, entry.ChangedAt, entry.ChangedBy, entry.Note))],
        order.TrackingNumber,
        order.CreatedAt);

    public static OrderSummaryDto ToSummary(this Order order) => new(
        order.Id, order.OrderNumber, order.Total, "LKR",
        order.PaymentStatus, order.OrderStatus,
        order.Items.Sum(item => item.Quantity), order.CreatedAt);
}
