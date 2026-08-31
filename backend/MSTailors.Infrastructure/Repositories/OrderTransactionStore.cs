using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MSTailors.Application.Orders;
using MSTailors.Domain.Entities;
using MSTailors.Infrastructure.Persistence;

namespace MSTailors.Infrastructure.Repositories;

/// <summary>
/// Places an order atomically.
/// </summary>
/// <remarks>
/// Two mechanisms work together, and both are needed:
///
/// 1. The conditional update. Each decrement filters on
///    `stockQuantity >= quantity`, so the check and the write are one operation
///    on the server. Two concurrent buyers cannot both pass a read-then-write
///    race, because there is no read.
///
/// 2. The transaction. A basket has several lines; without it, succeeding on
///    line one and failing on line three would leave stock decremented for a
///    order that was never created. Aborting rolls every decrement back.
///
/// Requires a replica set. Atlas is one by default; local development needs
/// mongod started with --replSet (see CLAUDE.md).
/// </remarks>
public sealed class OrderTransactionStore : IOrderTransactionStore
{
    private readonly IMongoClient _client;
    private readonly IMongoContext _context;
    private readonly ILogger<OrderTransactionStore> _logger;

    public OrderTransactionStore(
        IMongoClient client,
        IMongoContext context,
        ILogger<OrderTransactionStore> logger)
    {
        _client = client;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// How many times a transaction is retried after a transient conflict.
    /// </summary>
    /// <remarks>
    /// MongoDB reports a lost write race as a WriteConflict carrying the
    /// TransientTransactionError label, and does NOT retry on its own. Without
    /// this loop, concurrent buyers for the same variant mostly fail: stock
    /// stays correct, but sellable units go unsold and customers see spurious
    /// "please try again" errors under load.
    /// </remarks>
    private const int MaxTransactionAttempts = 8;

    public async Task<PlaceOrderOutcome> PlaceOrderAsync(
        Order order,
        IReadOnlyList<StockReservation> reservations,
        string? couponId,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await AttemptPlaceOrderAsync(order, reservations, couponId, cancellationToken);
            }
            catch (MongoException ex)
                when (ex.HasErrorLabel("TransientTransactionError") && attempt < MaxTransactionAttempts)
            {
                _logger.LogDebug(
                    "Checkout hit a transient transaction conflict on attempt {Attempt}; retrying", attempt);

                // Exponential backoff with jitter, so retrying contenders do
                // not immediately collide with each other again.
                var backoff = TimeSpan.FromMilliseconds(
                    Math.Pow(2, attempt) * 5 + Random.Shared.Next(0, 15));

                await Task.Delay(backoff, cancellationToken);
            }
        }
    }

    private async Task<PlaceOrderOutcome> AttemptPlaceOrderAsync(
        Order order,
        IReadOnlyList<StockReservation> reservations,
        string? couponId,
        CancellationToken cancellationToken)
    {
        using var session = await _client.StartSessionAsync(cancellationToken: cancellationToken);

        session.StartTransaction(new TransactionOptions(
            readConcern: ReadConcern.Snapshot,
            writeConcern: WriteConcern.WMajority));

        try
        {
            foreach (var reservation in reservations)
            {
                var decremented = await TryDecrementAsync(session, reservation, cancellationToken);

                if (!decremented)
                {
                    // Abort before writing anything else. Every earlier
                    // decrement in this transaction is undone by the abort.
                    await session.AbortTransactionAsync(cancellationToken);

                    _logger.LogInformation(
                        "Checkout aborted: insufficient stock for {Sku}", reservation.VariantSku);

                    return new PlaceOrderOutcome(false, null,
                        $"'{reservation.VariantSku}' does not have {reservation.Quantity} in stock.");
                }
            }

            if (couponId is not null && !await TryConsumeCouponAsync(session, couponId, cancellationToken))
            {
                await session.AbortTransactionAsync(cancellationToken);
                return new PlaceOrderOutcome(false, null, "That coupon has reached its usage limit.");
            }

            order.OrderNumber = await NextOrderNumberAsync(session, DateTime.UtcNow.Year, cancellationToken);

            var now = DateTime.UtcNow;
            order.CreatedAt = now;
            order.UpdatedAt = now;
            order.IsDeleted = false;

            await _context.Orders.InsertOneAsync(session, order, cancellationToken: cancellationToken);

            await CommitWithRetryAsync(session, cancellationToken);

            _logger.LogInformation("Order {OrderNumber} committed with {Lines} lines",
                order.OrderNumber, reservations.Count);

            return new PlaceOrderOutcome(true, order, null);
        }
        catch (Exception ex)
        {
            // Abort is best-effort: the transaction may already have ended.
            try
            {
                await session.AbortTransactionAsync(cancellationToken);
            }
            catch (Exception abortFailure)
            {
                _logger.LogWarning(abortFailure, "Failed to abort the checkout transaction");
            }

            _logger.LogError(ex, "Checkout transaction failed");
            throw;
        }
    }

    /// <summary>
    /// Commits, retrying while the server reports UnknownTransactionCommitResult.
    /// </summary>
    /// <remarks>
    /// That label means the commit may or may not have been applied — for
    /// example the primary stepped down mid-commit. Retrying is safe because
    /// committing an already-committed transaction is a no-op, whereas giving
    /// up could lose an order that actually went through.
    /// </remarks>
    private static async Task CommitWithRetryAsync(
        IClientSessionHandle session, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await session.CommitTransactionAsync(cancellationToken);
                return;
            }
            catch (MongoException ex)
                when (ex.HasErrorLabel("UnknownTransactionCommitResult") && attempt < MaxTransactionAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(10 * attempt), cancellationToken);
            }
        }
    }

    private async Task<bool> TryDecrementAsync(
        IClientSessionHandle session,
        StockReservation reservation,
        CancellationToken cancellationToken)
    {
        // The quantity check lives in the filter, not in a prior read. If two
        // checkouts race for the last item, the loser matches zero documents.
        var filter = Builders<Product>.Filter.And(
            Builders<Product>.Filter.Eq(product => product.Id, reservation.ProductId),
            Builders<Product>.Filter.Eq(product => product.IsDeleted, false),
            Builders<Product>.Filter.Eq(product => product.IsActive, true),
            Builders<Product>.Filter.ElemMatch(
                product => product.Variants,
                variant => variant.Sku == reservation.VariantSku
                           && variant.IsActive
                           && variant.StockQuantity >= reservation.Quantity));

        var update = Builders<Product>.Update
            .Inc("variants.$.stockQuantity", -reservation.Quantity)
            .Inc(product => product.SalesCount, reservation.Quantity)
            .Set(product => product.UpdatedAt, DateTime.UtcNow);

        var result = await _context.Products.UpdateOneAsync(
            session, filter, update, cancellationToken: cancellationToken);

        return result.ModifiedCount > 0;
    }

    private async Task<bool> TryConsumeCouponAsync(
        IClientSessionHandle session, string couponId, CancellationToken cancellationToken)
    {
        // Same conditional-update pattern: the usage limit is enforced by the
        // filter, so a coupon capped at N redemptions cannot be used N+1 times.
        var filter = Builders<Coupon>.Filter.And(
            Builders<Coupon>.Filter.Eq(coupon => coupon.Id, couponId),
            Builders<Coupon>.Filter.Eq(coupon => coupon.IsDeleted, false),
            Builders<Coupon>.Filter.Eq(coupon => coupon.IsActive, true),
            Builders<Coupon>.Filter.Or(
                Builders<Coupon>.Filter.Eq(coupon => coupon.UsageLimit, null),
                Builders<Coupon>.Filter.Where(coupon => coupon.UsedCount < coupon.UsageLimit)));

        var result = await _context.Coupons.UpdateOneAsync(
            session,
            filter,
            Builders<Coupon>.Update
                .Inc(coupon => coupon.UsedCount, 1)
                .Set(coupon => coupon.UpdatedAt, DateTime.UtcNow),
            cancellationToken: cancellationToken);

        return result.ModifiedCount > 0;
    }

    private async Task<string> NextOrderNumberAsync(
        IClientSessionHandle session, int year, CancellationToken cancellationToken)
    {
        var counters = _context.GetCollection<BsonDocument>("counters");

        var result = await counters.FindOneAndUpdateAsync(
            session,
            Builders<BsonDocument>.Filter.Eq("_id", $"orders-{year}"),
            Builders<BsonDocument>.Update.Inc("sequence", 1),
            new FindOneAndUpdateOptions<BsonDocument>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            },
            cancellationToken);

        return $"MST-{year}-{result["sequence"].ToInt64():D4}";
    }
}

public sealed class CouponRepository : MongoRepository<Coupon>, MSTailors.Application.Carts.ICouponRepository
{
    public CouponRepository(IMongoContext context) : base(context.Coupons)
    {
    }

    public async Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var filter = Filter.And(
            NotDeleted,
            Filter.Eq(coupon => coupon.Code, code.Trim().ToUpperInvariant()));

        return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> TryConsumeAsync(string couponId, CancellationToken cancellationToken = default)
    {
        if (!IsValidObjectId(couponId))
        {
            return false;
        }

        var filter = Filter.And(
            ById(couponId),
            Filter.Eq(coupon => coupon.IsActive, true),
            Filter.Or(
                Filter.Eq(coupon => coupon.UsageLimit, null),
                Filter.Where(coupon => coupon.UsedCount < coupon.UsageLimit)));

        var result = await Collection.UpdateOneAsync(
            filter,
            Update.Inc(coupon => coupon.UsedCount, 1).Set(coupon => coupon.UpdatedAt, DateTime.UtcNow),
            cancellationToken: cancellationToken);

        return result.ModifiedCount > 0;
    }

    public override async Task<Coupon> AddAsync(Coupon entity, CancellationToken cancellationToken = default)
    {
        entity.Code = entity.Code.Trim().ToUpperInvariant();
        return await base.AddAsync(entity, cancellationToken);
    }
}
