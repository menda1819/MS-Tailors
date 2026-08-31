using MongoDB.Driver;
using MSTailors.Application.Common.Repositories;
using MSTailors.Domain.Entities;
using MSTailors.Infrastructure.Persistence;

namespace MSTailors.Infrastructure.Repositories;

public sealed class CustomerRepository : MongoRepository<Customer>, ICustomerRepository
{
    public CustomerRepository(IMongoContext context) : base(context.Customers)
    {
    }

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var filter = Filter.And(
            NotDeleted,
            Filter.Eq(customer => customer.Email, email.Trim().ToLowerInvariant()));

        return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> IsEmailAvailableAsync(
        string email,
        string? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var filter = Filter.And(
            NotDeleted,
            Filter.Eq(customer => customer.Email, email.Trim().ToLowerInvariant()));

        if (!string.IsNullOrWhiteSpace(excludeId) && IsValidObjectId(excludeId))
        {
            filter = Filter.And(filter, Filter.Ne(customer => customer.Id, excludeId));
        }

        return !await Collection.Find(filter).AnyAsync(cancellationToken);
    }

    public async Task<Customer?> GetByRefreshTokenAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            return null;
        }

        var filter = Filter.And(
            NotDeleted,
            Filter.ElemMatch(customer => customer.RefreshTokens, token => token.Token == tokenHash));

        return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public override async Task<Customer> AddAsync(Customer entity, CancellationToken cancellationToken = default)
    {
        // Normalising here means the unique index on email cannot be defeated by casing.
        entity.Email = entity.Email.Trim().ToLowerInvariant();
        return await base.AddAsync(entity, cancellationToken);
    }
}
