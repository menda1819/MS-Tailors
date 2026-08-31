using MongoDB.Bson;
using MongoDB.Driver;
using MSTailors.Application.Common.Pagination;
using MSTailors.Application.Orders;
using MSTailors.Domain.Entities;
using MSTailors.Domain.Enums;
using MSTailors.Domain.ValueObjects;
using MSTailors.Infrastructure.Persistence;

namespace MSTailors.Infrastructure.Repositories;

public sealed class OrderRepository : MongoRepository<Order>, IOrderRepository
{
    private readonly IMongoCollection<BsonDocument> _counters;

    public OrderRepository(IMongoContext context) : base(context.Orders)
    {
        _counters = context.GetCollection<BsonDocument>("counters");
    }

    public async Task<Order?> GetByOrderNumberAsync(
        string orderNumber,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            return null;
        }

        var filter = Filter.And(
            NotDeleted,
            Filter.Eq(order => order.OrderNumber, orderNumber.Trim().ToUpperInvariant()));

        return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Order?> GetForGuestAsync(
        string orderNumber,
        string email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderNumber) || string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        // Both must match. The order number alone is guessable, so it is not a
        // sufficient credential for exposing a customer's address and contact details.
        var filter = Filter.And(
            NotDeleted,
            Filter.Eq(order => order.OrderNumber, orderNumber.Trim().ToUpperInvariant()),
            Filter.Eq(order => order.Contact.Email, email.Trim().ToLowerInvariant()));

        return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<Order>> SearchAsync(
        OrderQuery query,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize) = NormalisePaging(query.Page, query.PageSize);

        var clauses = new List<FilterDefinition<Order>> { NotDeleted };

        if (!string.IsNullOrWhiteSpace(query.CustomerId) && IsValidObjectId(query.CustomerId))
        {
            clauses.Add(Filter.Eq(order => order.CustomerId, query.CustomerId));
        }

        if (!string.IsNullOrWhiteSpace(query.Email))
        {
            clauses.Add(Filter.Eq(order => order.Contact.Email, query.Email.Trim().ToLowerInvariant()));
        }

        if (query.OrderStatus is { } orderStatus)
        {
            clauses.Add(Filter.Eq(order => order.OrderStatus, orderStatus));
        }

        if (query.PaymentStatus is { } paymentStatus)
        {
            clauses.Add(Filter.Eq(order => order.PaymentStatus, paymentStatus));
        }

        if (query.PlacedFrom is { } from)
        {
            clauses.Add(Filter.Gte(order => order.CreatedAt, from));
        }

        if (query.PlacedTo is { } to)
        {
            clauses.Add(Filter.Lt(order => order.CreatedAt, to));
        }

        var filter = Filter.And(clauses);

        var total = await Collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var items = await Collection
            .Find(filter)
            .SortByDescending(order => order.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Order>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<Order>> GetByCustomerAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidObjectId(customerId))
        {
            return [];
        }

        var filter = Filter.And(NotDeleted, Filter.Eq(order => order.CustomerId, customerId));
        return await Collection.Find(filter)
            .SortByDescending(order => order.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ChangeStatusAsync(
        string orderId,
        OrderStatus status,
        string changedBy,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidObjectId(orderId))
        {
            return false;
        }

        var entry = new OrderStatusChange
        {
            Status = status,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = string.IsNullOrWhiteSpace(changedBy) ? "system" : changedBy,
            Note = note
        };

        // Status and history move together in one write, so the audit trail can
        // never disagree with the current status.
        var update = Update
            .Set(order => order.OrderStatus, status)
            .Push(order => order.StatusHistory, entry)
            .Set(order => order.UpdatedAt, DateTime.UtcNow);

        var result = await Collection.UpdateOneAsync(ById(orderId), update, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> SetPaymentStatusAsync(
        string orderId,
        PaymentStatus status,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidObjectId(orderId))
        {
            return false;
        }

        var update = Update
            .Set(order => order.PaymentStatus, status)
            .Set(order => order.UpdatedAt, DateTime.UtcNow);

        var result = await Collection.UpdateOneAsync(ById(orderId), update, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }

    public async Task<string> GenerateOrderNumberAsync(int year, CancellationToken cancellationToken = default)
    {
        // findOneAndUpdate with $inc is atomic on the server, so concurrent
        // checkouts get distinct numbers without any application-side locking.
        var result = await _counters.FindOneAndUpdateAsync<BsonDocument>(
            Builders<BsonDocument>.Filter.Eq("_id", $"orders-{year}"),
            Builders<BsonDocument>.Update.Inc("sequence", 1),
            new FindOneAndUpdateOptions<BsonDocument>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            },
            cancellationToken);

        var sequence = result["sequence"].ToInt64();
        return $"MST-{year}-{sequence:D4}";
    }
}
