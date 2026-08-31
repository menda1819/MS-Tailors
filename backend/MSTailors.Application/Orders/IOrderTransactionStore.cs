using MSTailors.Domain.Entities;

namespace MSTailors.Application.Orders;

/// <summary>One variant's stock reservation for a checkout.</summary>
public sealed record StockReservation(string ProductId, string VariantSku, int Quantity);

public sealed record PlaceOrderOutcome(bool Succeeded, Order? Order, string? FailureReason);

/// <summary>
/// Writes an order and its stock movements as one atomic unit.
/// </summary>
/// <remarks>
/// Lives in Infrastructure because it owns the MongoDB session and transaction.
/// Application computes what the order should be; this decides whether it can
/// actually be committed.
/// </remarks>
public interface IOrderTransactionStore
{
    /// <summary>
    /// Inside a single transaction: allocates the order number, decrements each
    /// variant's stock with a conditional update, increments sales counts, and
    /// inserts the order. Any insufficient-stock condition aborts the whole
    /// transaction, so a partially fulfilled order can never be persisted.
    /// </summary>
    Task<PlaceOrderOutcome> PlaceOrderAsync(
        Order order,
        IReadOnlyList<StockReservation> reservations,
        string? couponId,
        CancellationToken cancellationToken = default);
}
