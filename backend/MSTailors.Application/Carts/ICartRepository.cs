using MSTailors.Application.Common.Repositories;
using MSTailors.Domain.Entities;

namespace MSTailors.Application.Carts;

public interface ICartRepository : IRepository<Cart>
{
    Task<Cart?> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default);

    Task<Cart?> GetByAnonymousIdAsync(string anonymousId, CancellationToken cancellationToken = default);

    /// <summary>Adds a line, or increases quantity when the same variant is already present.</summary>
    Task<Cart> AddItemAsync(string cartId, Domain.ValueObjects.CartItem item, CancellationToken cancellationToken = default);

    /// <summary>Sets quantity for a line, removing it when quantity reaches zero.</summary>
    Task<Cart?> SetItemQuantityAsync(string cartId, string variantSku, int quantity, CancellationToken cancellationToken = default);

    Task<Cart?> RemoveItemAsync(string cartId, string variantSku, CancellationToken cancellationToken = default);

    Task<bool> ClearAsync(string cartId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves an anonymous cart to a customer on sign-in, merging quantities when
    /// the customer already has a cart. Returns the surviving cart.
    /// </summary>
    Task<Cart?> MergeAnonymousIntoCustomerAsync(string anonymousId, string customerId, CancellationToken cancellationToken = default);

    /// <summary>Pushes expiresAt forward so an active cart is not reaped by the TTL index.</summary>
    Task<bool> TouchAsync(string cartId, TimeSpan lifetime, CancellationToken cancellationToken = default);
}
