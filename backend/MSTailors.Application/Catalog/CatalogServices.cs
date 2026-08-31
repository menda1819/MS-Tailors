using MSTailors.Application.Catalog.Dtos;
using MSTailors.Application.Common.Dtos;
using MSTailors.Application.Common.Exceptions;
using MSTailors.Application.Common.Mapping;
using MSTailors.Application.Common.Repositories;
using MSTailors.Domain.Entities;

namespace MSTailors.Application.Catalog;

public sealed class BrandService : IBrandService
{
    private readonly ISlugRepository<Brand> _brands;
    private readonly IProductRepository _products;

    public BrandService(ISlugRepository<Brand> brands, IProductRepository products)
    {
        _brands = brands;
        _products = products;
    }

    public async Task<PagedResponse<BrandListItemDto>> ListAsync(
        PageRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = await _brands.PageAsync(
            request.Page, request.PageSize, brand => brand.IsActive, cancellationToken);

        // Curated order, not insertion order.
        var ordered = page.Items.OrderBy(brand => brand.DisplayOrder).ThenBy(brand => brand.Name).ToList();

        return PagedResponse<BrandListItemDto>.From(
            page with { Items = ordered }, brand => brand.ToListItem());
    }

    public async Task<BrandDetailDto> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var brand = await _brands.GetBySlugAsync(slug, cancellationToken)
            ?? throw new NotFoundException("Brand", slug);

        if (!brand.IsActive)
        {
            throw new NotFoundException("Brand", slug);
        }

        var productCount = await _products.CountAsync(
            product => product.BrandId == brand.Id && product.IsActive, cancellationToken);

        return brand.ToDetail(productCount);
    }
}

public sealed class CategoryService : ICategoryService
{
    private readonly ISlugRepository<Category> _categories;

    public CategoryService(ISlugRepository<Category> categories)
    {
        _categories = categories;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        var all = await _categories.ListAsync(category => category.IsActive, cancellationToken);

        var byParent = all
            .Where(category => category.ParentCategoryId is not null)
            .GroupBy(category => category.ParentCategoryId!)
            .ToDictionary(group => group.Key, group => group.ToList());

        return
        [
            .. all
                .Where(category => category.ParentCategoryId is null)
                .OrderBy(category => category.DisplayOrder)
                .ThenBy(category => category.Name)
                .Select(parent => parent.ToDto(
                [
                    .. (byParent.TryGetValue(parent.Id, out var children) ? children : [])
                        .OrderBy(child => child.DisplayOrder)
                        .ThenBy(child => child.Name)
                        .Select(child => child.ToDto())
                ]))
        ];
    }
}

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _products;
    private readonly ISlugRepository<Brand> _brands;
    private readonly ISlugRepository<Category> _categories;

    public ProductService(
        IProductRepository products,
        ISlugRepository<Brand> brands,
        ISlugRepository<Category> categories)
    {
        _products = products;
        _brands = brands;
        _categories = categories;
    }

    public async Task<PagedResponse<ProductListItemDto>> SearchAsync(
        ProductQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        // Links in the UI carry slugs; the admin carries ids. Accept either.
        var brandId = await ResolveBrandIdAsync(request.Brand, cancellationToken);
        var categoryId = await ResolveCategoryIdAsync(request.Category, cancellationToken);

        // An unknown brand or category slug is an empty result, not everything.
        if ((request.Brand is not null && brandId is null) ||
            (request.Category is not null && categoryId is null))
        {
            return PagedResponse<ProductListItemDto>.From(
                Common.Pagination.PagedResult<Product>.Empty(request.Page, request.PageSize),
                product => product.ToListItem());
        }

        var query = new ProductQuery
        {
            BrandId = brandId,
            CategoryId = categoryId,
            Size = request.Size,
            Color = request.Color,
            MinPrice = request.MinPrice,
            MaxPrice = request.MaxPrice,
            InStockOnly = request.InStock == true,
            IsNewArrival = request.IsNew,
            IsFeatured = request.IsFeatured,
            SearchTerm = request.Q,
            Sort = request.Sort switch
            {
                ProductSortOption.PriceAsc => ProductSort.PriceAscending,
                ProductSortOption.PriceDesc => ProductSort.PriceDescending,
                ProductSortOption.Popular => ProductSort.MostViewed,
                ProductSortOption.BestSelling => ProductSort.BestSelling,
                _ => ProductSort.Newest
            },
            Page = request.Page,
            PageSize = request.PageSize
        };

        var page = await _products.SearchAsync(query, cancellationToken);
        return PagedResponse<ProductListItemDto>.From(page, product => product.ToListItem());
    }

    public async Task<ProductDetailDto> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var product = await LoadActiveAsync(slug, cancellationToken);

        // Best-effort: a failed counter must never fail the page.
        await _products.IncrementViewCountAsync(product.Id, cancellationToken);

        return product.ToDetail();
    }

    public async Task<IReadOnlyList<ProductListItemDto>> GetRelatedAsync(
        string slug,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var product = await LoadActiveAsync(slug, cancellationToken);
        var related = await _products.GetRelatedAsync(product, limit, cancellationToken);

        return [.. related.Select(item => item.ToListItem())];
    }

    private async Task<Product> LoadActiveAsync(string slug, CancellationToken cancellationToken)
    {
        var product = await _products.GetBySlugAsync(slug, cancellationToken)
            ?? throw new NotFoundException("Product", slug);

        // An inactive product is invisible to the public API, not a 200 with a hidden flag.
        return product.IsActive ? product : throw new NotFoundException("Product", slug);
    }

    private async Task<string?> ResolveBrandIdAsync(string? brandOrSlug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(brandOrSlug))
        {
            return null;
        }

        var byId = await _brands.GetByIdAsync(brandOrSlug, cancellationToken);
        if (byId is not null)
        {
            return byId.Id;
        }

        return (await _brands.GetBySlugAsync(brandOrSlug, cancellationToken))?.Id;
    }

    private async Task<string?> ResolveCategoryIdAsync(string? categoryOrSlug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(categoryOrSlug))
        {
            return null;
        }

        var byId = await _categories.GetByIdAsync(categoryOrSlug, cancellationToken);
        if (byId is not null)
        {
            return byId.Id;
        }

        return (await _categories.GetBySlugAsync(categoryOrSlug, cancellationToken))?.Id;
    }
}
