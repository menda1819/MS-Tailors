using MSTailors.Application.Common.Pagination;
using MSTailors.Application.Common.Repositories;
using MSTailors.Domain.Entities;
using MSTailors.Domain.Enums;

namespace MSTailors.Application.Orders;

public sealed record OrderQuery
{
    public string? CustomerId { get; init; }
    public string? Email { get; init; }
    public OrderStatus? OrderStatus { get; init; }
    public PaymentStatus? PaymentStatus { get; init; }
    public DateTime? PlacedFrom { get; init; }
    public DateTime? PlacedTo { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);

    /// <summary>Guest order lookup. Email must match the order's contact email.</summary>
    Task<Order?> GetForGuestAsync(string orderNumber, string email, CancellationToken cancellationToken = default);

    Task<PagedResult<Order>> SearchAsync(OrderQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> GetByCustomerAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>Appends to statusHistory and updates orderStatus in one write.</summary>
    Task<bool> ChangeStatusAsync(
        string orderId,
        OrderStatus status,
        string changedBy,
        string? note = null,
        CancellationToken cancellationToken = default);

    Task<bool> SetPaymentStatusAsync(string orderId, PaymentStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Allocates the next order number for a year, e.g. MST-2026-0001.
    /// Must be atomic — two concurrent checkouts cannot receive the same number.
    /// </summary>
    Task<string> GenerateOrderNumberAsync(int year, CancellationToken cancellationToken = default);
}
