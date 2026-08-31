using MongoDB.Bson;
using MongoDB.Driver;
using MSTailors.Application.Catalog;
using MSTailors.Application.Common.Pagination;
using MSTailors.Domain.Entities;
using MSTailors.Infrastructure.Persistence;

namespace MSTailors.Infrastructure.Repositories;

public sealed class ProductRepository : MongoSlugRepository<Product>, IProductRepository
{
    public ProductRepository(IMongoContext context) : base(context.Products)
    {
    }

    public async Task<PagedResult<Product>> SearchAsync(
        ProductQuery query,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize) = NormalisePaging(query.Page, query.PageSize);

        var clauses = new List<FilterDefinition<Product>>
        {
            NotDeleted,
            Filter.Eq(product => product.IsActive, true)
        };

        if (!string.IsNullOrWhiteSpace(query.BrandId) && IsValidObjectId(query.BrandId))
        {
            clauses.Add(Filter.Eq(product => product.BrandId, query.BrandId));
        }

        if (!string.IsNullOrWhiteSpace(query.CategoryId) && IsValidObjectId(query.CategoryId))
        {
            clauses.Add(Filter.AnyEq(product => product.CategoryIds, query.CategoryId));
        }

        if (query.Tags is { Count: > 0 })
        {
            clauses.Add(Filter.AnyIn(product => product.Tags, query.Tags));
        }

        if (query.IsFeatured is { } featured)
        {
            clauses.Add(Filter.Eq(product => product.IsFeatured, featured));
        }

        if (query.IsNewArrival is { } newArrival)
        {
            clauses.Add(Filter.Eq(product => product.IsNewArrival, newArrival));
        }

        if (!string.IsNullOrWhiteSpace(query.Size))
        {
            clauses.Add(Filter.ElemMatch(product => product.Variants,
                variant => variant.Size == query.Size));
        }

        if (!string.IsNullOrWhiteSpace(query.Color))
        {
            clauses.Add(Filter.ElemMatch(product => product.Variants,
                variant => variant.Color == query.Color));
        }

        if (query.InStockOnly)
        {
            // Stock lives on variants, so "in stock" means at least one variant
            // has quantity > 0 — not a product-level field.
            clauses.Add(Filter.ElemMatch(product => product.Variants, variant => variant.StockQuantity > 0));
        }

        // Price filtering uses the effective price: salePrice when present, else basePrice.
        if (query.MinPrice is { } min)
        {
            clauses.Add(EffectivePriceAtLeast(min));
        }

        if (query.MaxPrice is { } max)
        {
            clauses.Add(EffectivePriceAtMost(max));
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            clauses.Add(Filter.Text(query.SearchTerm.Trim()));
        }

        var filter = Filter.And(clauses);

        var total = await Collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var find = Collection.Find(filter);

        find = query.Sort switch
        {
            ProductSort.PriceAscending => find.SortBy(product => product.BasePrice),
            ProductSort.PriceDescending => find.SortByDescending(product => product.BasePrice),
            ProductSort.NameAscending => find.SortBy(product => product.Name),
            ProductSort.MostViewed => find.SortByDescending(product => product.ViewCount),
            ProductSort.BestSelling => find.SortByDescending(product => product.SalesCount),
            _ => find.SortByDescending(product => product.CreatedAt)
        };

        var items = await find
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Product>(items, page, pageSize, total);
    }

    /// <summary>
    /// effectivePrice >= value, where effectivePrice is salePrice if set else basePrice.
    /// Expressed as an OR because Mongo cannot compare two fields in a plain filter.
    /// </summary>
    private static FilterDefinition<Product> EffectivePriceAtLeast(decimal value) =>
        Filter.Or(
            Filter.And(
                Filter.Ne("salePrice", BsonNull.Value),
                Filter.Exists("salePrice"),
                Filter.Gte("salePrice", value)),
            Filter.And(
                Filter.Or(Filter.Eq("salePrice", BsonNull.Value), Filter.Exists("salePrice", false)),
                Filter.Gte("basePrice", value)));

    private static FilterDefinition<Product> EffectivePriceAtMost(decimal value) =>
        Filter.Or(
            Filter.And(
                Filter.Ne("salePrice", BsonNull.Value),
                Filter.Exists("salePrice"),
                Filter.Lte("salePrice", value)),
            Filter.And(
                Filter.Or(Filter.Eq("salePrice", BsonNull.Value), Filter.Exists("salePrice", false)),
                Filter.Lte("basePrice", value)));

    public async Task<Product?> GetByVariantSkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return null;
        }

        var filter = Filter.And(
            NotDeleted,
            Filter.ElemMatch(product => product.Variants, variant => variant.Sku == sku));

        return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetByBrandAsync(
        string brandId,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidObjectId(brandId))
        {
            return [];
        }

        var filter = Filter.And(NotDeleted, Filter.Eq(product => product.BrandId, brandId));
        return await Collection.Find(filter).SortBy(product => product.Name).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(
        IEnumerable<string> ids,
        CancellationToken cancellationToken = default)
    {
        var valid = ids.Where(IsValidObjectId).Distinct().ToList();
        if (valid.Count == 0)
        {
            return [];
        }

        var filter = Filter.And(NotDeleted, Filter.In(product => product.Id, valid));
        return await Collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<bool> IsSkuAvailableAsync(
        string sku,
        string? excludeProductId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return false;
        }

        var filter = Filter.And(
            NotDeleted,
            Filter.ElemMatch(product => product.Variants, variant => variant.Sku == sku));

        if (!string.IsNullOrWhiteSpace(excludeProductId) && IsValidObjectId(excludeProductId))
        {
            filter = Filter.And(filter, Filter.Ne(product => product.Id, excludeProductId));
        }

        return !await Collection.Find(filter).AnyAsync(cancellationToken);
    }

    public async Task<bool> TryDecrementStockAsync(
        string productId,
        string sku,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidObjectId(productId) || quantity <= 0)
        {
            return false;
        }

        // The stockQuantity >= quantity condition is part of the filter, not a
        // prior read. Two concurrent checkouts therefore cannot both succeed on
        // the last item — the second one matches nothing.
        var filter = Filter.And(
            NotDeleted,
            Filter.Eq(product => product.Id, productId),
            Filter.ElemMatch(
                product => product.Variants,
                variant => variant.Sku == sku && variant.StockQuantity >= quantity));

        var update = Update
            .Inc("variants.$.stockQuantity", -quantity)
            .Set(product => product.UpdatedAt, DateTime.UtcNow);

        var result = await Collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> IncrementStockAsync(
        string productId,
        string sku,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidObjectId(productId) || quantity <= 0)
        {
            return false;
        }

        var filter = Filter.And(
            NotDeleted,
            Filter.Eq(product => product.Id, productId),
            Filter.ElemMatch(product => product.Variants, variant => variant.Sku == sku));

        var update = Update
            .Inc("variants.$.stockQuantity", quantity)
            .Set(product => product.UpdatedAt, DateTime.UtcNow);

        var result = await Collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }

    public async Task<IReadOnlyList<Product>> GetRelatedAsync(
        Product product,
        int limit,
        CancellationToken cancellationToken = default)
    {
        // Same brand or an overlapping category, never the product itself.
        var filter = Filter.And(
            NotDeleted,
            Filter.Eq(candidate => candidate.IsActive, true),
            Filter.Ne(candidate => candidate.Id, product.Id),
            Filter.Or(
                Filter.Eq(candidate => candidate.BrandId, product.BrandId),
                Filter.AnyIn(candidate => candidate.CategoryIds, product.CategoryIds)));

        return await Collection.Find(filter)
            .SortByDescending(candidate => candidate.SalesCount)
            .Limit(Math.Clamp(limit, 1, 24))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetLowStockAsync(
        int threshold,
        CancellationToken cancellationToken = default)
    {
        var filter = Filter.And(
            NotDeleted,
            Filter.Eq(product => product.IsActive, true),
            Filter.ElemMatch(product => product.Variants,
                variant => variant.IsActive && variant.StockQuantity <= threshold));

        return await Collection.Find(filter)
            .SortBy(product => product.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetBestSellersAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        var filter = Filter.And(NotDeleted, Filter.Eq(product => product.IsActive, true));

        return await Collection.Find(filter)
            .SortByDescending(product => product.SalesCount)
            .Limit(Math.Clamp(limit, 1, 50))
            .ToListAsync(cancellationToken);
    }

    public async Task IncrementViewCountAsync(string productId, CancellationToken cancellationToken = default)
    {
        if (!IsValidObjectId(productId))
        {
            return;
        }

        // UpdatedAt is deliberately not touched: a view is not an edit, and
        // bumping it would invalidate caches keyed on content changes.
        await Collection.UpdateOneAsync(
            ById(productId),
            Update.Inc(product => product.ViewCount, 1),
            cancellationToken: cancellationToken);
    }
}
