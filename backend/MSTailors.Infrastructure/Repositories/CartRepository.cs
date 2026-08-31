using MongoDB.Driver;
using MSTailors.Application.Carts;
using MSTailors.Domain.Entities;
using MSTailors.Domain.ValueObjects;
using MSTailors.Infrastructure.Persistence;

namespace MSTailors.Infrastructure.Repositories;

public sealed class CartRepository : MongoRepository<Cart>, ICartRepository
{
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromDays(30);

    public CartRepository(IMongoContext context) : base(context.Carts)
    {
    }

    public async Task<Cart?> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default)
    {
        if (!IsValidObjectId(customerId))
        {
            return null;
        }

        var filter = Filter.And(NotDeleted, Filter.Eq(cart => cart.CustomerId, customerId));
        return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Cart?> GetByAnonymousIdAsync(string anonymousId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(anonymousId))
        {
            return null;
        }

        var filter = Filter.And(NotDeleted, Filter.Eq(cart => cart.AnonymousId, anonymousId));
        return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Cart> AddItemAsync(string cartId, CartItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.Quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(item), "Quantity must be greater than zero.");
        }

        var expiry = DateTime.UtcNow.Add(DefaultLifetime);

        // Try to bump an existing line first. Matching on the variant sku means
        // adding the same size/colour twice increments rather than duplicating.
        var existingFilter = Filter.And(
            ById(cartId),
            Filter.ElemMatch(cart => cart.Items, line => line.VariantSku == item.VariantSku));

        var bumped = await Collection.FindOneAndUpdateAsync(
            existingFilter,
            Update
                .Inc("items.$.quantity", item.Quantity)
                .Set("items.$.unitPriceSnapshot", item.UnitPriceSnapshot)
                .Set(cart => cart.UpdatedAt, DateTime.UtcNow)
                .Set(cart => cart.ExpiresAt, expiry),
            new FindOneAndUpdateOptions<Cart> { ReturnDocument = ReturnDocument.After },
            cancellationToken);

        if (bumped is not null)
        {
            return bumped;
        }

        var appended = await Collection.FindOneAndUpdateAsync(
            ById(cartId),
            Update
                .Push(cart => cart.Items, item)
                .Set(cart => cart.UpdatedAt, DateTime.UtcNow)
                .Set(cart => cart.ExpiresAt, expiry),
            new FindOneAndUpdateOptions<Cart> { ReturnDocument = ReturnDocument.After },
            cancellationToken);

        return appended ?? throw new InvalidOperationException($"Cart '{cartId}' was not found.");
    }

    public async Task<Cart?> SetItemQuantityAsync(
        string cartId,
        string variantSku,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidObjectId(cartId) || string.IsNullOrWhiteSpace(variantSku))
        {
            return null;
        }

        // Quantity zero means remove, so the caller never has to special-case it.
        if (quantity <= 0)
        {
            return await RemoveItemAsync(cartId, variantSku, cancellationToken);
        }

        var filter = Filter.And(
            ById(cartId),
            Filter.ElemMatch(cart => cart.Items, line => line.VariantSku == variantSku));

        return await Collection.FindOneAndUpdateAsync(
            filter,
            Update
                .Set("items.$.quantity", quantity)
                .Set(cart => cart.UpdatedAt, DateTime.UtcNow)
                .Set(cart => cart.ExpiresAt, DateTime.UtcNow.Add(DefaultLifetime)),
            new FindOneAndUpdateOptions<Cart> { ReturnDocument = ReturnDocument.After },
            cancellationToken);
    }

    public async Task<Cart?> RemoveItemAsync(
        string cartId,
        string variantSku,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidObjectId(cartId) || string.IsNullOrWhiteSpace(variantSku))
        {
            return null;
        }

        return await Collection.FindOneAndUpdateAsync(
            ById(cartId),
            Update
                .PullFilter(cart => cart.Items, line => line.VariantSku == variantSku)
                .Set(cart => cart.UpdatedAt, DateTime.UtcNow),
            new FindOneAndUpdateOptions<Cart> { ReturnDocument = ReturnDocument.After },
            cancellationToken);
    }

    public async Task<bool> ClearAsync(string cartId, CancellationToken cancellationToken = default)
    {
        if (!IsValidObjectId(cartId))
        {
            return false;
        }

        var result = await Collection.UpdateOneAsync(
            ById(cartId),
            Update
                .Set(cart => cart.Items, [])
                .Unset(cart => cart.CouponCode)
                .Set(cart => cart.UpdatedAt, DateTime.UtcNow),
            cancellationToken: cancellationToken);

        return result.ModifiedCount > 0;
    }

    public async Task<Cart?> MergeAnonymousIntoCustomerAsync(
        string anonymousId,
        string customerId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(anonymousId) || !IsValidObjectId(customerId))
        {
            return null;
        }

        var anonymousCart = await GetByAnonymousIdAsync(anonymousId, cancellationToken);
        var customerCart = await GetByCustomerIdAsync(customerId, cancellationToken);

        if (anonymousCart is null)
        {
            return customerCart;
        }

        // No existing customer cart: adopt the anonymous one rather than copying it.
        if (customerCart is null)
        {
            anonymousCart.CustomerId = customerId;
            anonymousCart.AnonymousId = null;
            anonymousCart.UpdatedAt = DateTime.UtcNow;
            anonymousCart.ExpiresAt = DateTime.UtcNow.Add(DefaultLifetime);

            await Collection.ReplaceOneAsync(
                ById(anonymousCart.Id), anonymousCart, cancellationToken: cancellationToken);

            return anonymousCart;
        }

        // Both exist: sum quantities per variant, keeping the customer cart's
        // price snapshots since that is the cart they were last looking at.
        foreach (var incoming in anonymousCart.Items)
        {
            var existing = customerCart.Items.FirstOrDefault(line => line.VariantSku == incoming.VariantSku);
            if (existing is null)
            {
                customerCart.Items.Add(incoming);
            }
            else
            {
                existing.Quantity += incoming.Quantity;
            }
        }

        customerCart.CouponCode ??= anonymousCart.CouponCode;
        customerCart.UpdatedAt = DateTime.UtcNow;
        customerCart.ExpiresAt = DateTime.UtcNow.Add(DefaultLifetime);

        await Collection.ReplaceOneAsync(
            ById(customerCart.Id), customerCart, cancellationToken: cancellationToken);

        // The anonymous cart is soft-deleted, not dropped, so a merge can be audited.
        await SoftDeleteAsync(anonymousCart.Id, cancellationToken);

        return customerCart;
    }

    public async Task<bool> TouchAsync(string cartId, TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        if (!IsValidObjectId(cartId))
        {
            return false;
        }

        var result = await Collection.UpdateOneAsync(
            ById(cartId),
            Update.Set(cart => cart.ExpiresAt, DateTime.UtcNow.Add(lifetime)),
            cancellationToken: cancellationToken);

        return result.ModifiedCount > 0;
    }
}
