using MSTailors.Application.Admin.Dtos;
using MSTailors.Application.Catalog;
using MSTailors.Application.Catalog.Dtos;
using MSTailors.Application.Common.Dtos;
using MSTailors.Application.Common.Exceptions;
using MSTailors.Application.Common.Mapping;
using MSTailors.Application.Common.Repositories;
using MSTailors.Domain.Entities;
using MSTailors.Domain.ValueObjects;

namespace MSTailors.Application.Admin;

public interface IAdminCatalogService
{
    // Brands
    Task<PagedResponse<BrandDetailDto>> ListBrandsAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<BrandDetailDto> CreateBrandAsync(BrandRequest request, CancellationToken cancellationToken = default);
    Task<BrandDetailDto> UpdateBrandAsync(string id, BrandRequest request, CancellationToken cancellationToken = default);
    Task DeleteBrandAsync(string id, CancellationToken cancellationToken = default);

    // Categories
    Task<IReadOnlyList<CategoryDto>> ListCategoriesAsync(CancellationToken cancellationToken = default);
    Task<CategoryDto> CreateCategoryAsync(CategoryRequest request, CancellationToken cancellationToken = default);
    Task<CategoryDto> UpdateCategoryAsync(string id, CategoryRequest request, CancellationToken cancellationToken = default);
    Task DeleteCategoryAsync(string id, CancellationToken cancellationToken = default);

    // Products
    Task<PagedResponse<ProductDetailDto>> ListProductsAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> GetProductAsync(string id, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> CreateProductAsync(ProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> UpdateProductAsync(string id, ProductRequest request, CancellationToken cancellationToken = default);
    Task DeleteProductAsync(string id, CancellationToken cancellationToken = default);

    // Variants
    Task<ProductDetailDto> AddVariantAsync(string productId, VariantRequest request, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> UpdateVariantAsync(string productId, string sku, VariantRequest request, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> DeleteVariantAsync(string productId, string sku, CancellationToken cancellationToken = default);
}

public sealed class AdminCatalogService : IAdminCatalogService
{
    private readonly ISlugRepository<Brand> _brands;
    private readonly ISlugRepository<Category> _categories;
    private readonly IProductRepository _products;

    public AdminCatalogService(
        ISlugRepository<Brand> brands, ISlugRepository<Category> categories, IProductRepository products)
    {
        _brands = brands;
        _categories = categories;
        _products = products;
    }

    // ---- Brands ------------------------------------------------------------

    public async Task<PagedResponse<BrandDetailDto>> ListBrandsAsync(
        PageRequest request, CancellationToken cancellationToken = default)
    {
        // Admin sees inactive brands too — that is the point of the admin list.
        var page = await _brands.PageAsync(request.Page, request.PageSize, null, cancellationToken);
        return PagedResponse<BrandDetailDto>.From(page, brand => brand.ToDetail(0));
    }

    public async Task<BrandDetailDto> CreateBrandAsync(
        BrandRequest request, CancellationToken cancellationToken = default)
    {
        var slug = Normalise(request.Slug);
        await EnsureBrandSlugFreeAsync(slug, null, cancellationToken);

        var brand = Apply(new Brand(), request, slug);
        var created = await _brands.AddAsync(brand, cancellationToken);

        return created.ToDetail(0);
    }

    public async Task<BrandDetailDto> UpdateBrandAsync(
        string id, BrandRequest request, CancellationToken cancellationToken = default)
    {
        var brand = await _brands.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Brand", id);

        var slug = Normalise(request.Slug);
        await EnsureBrandSlugFreeAsync(slug, id, cancellationToken);

        Apply(brand, request, slug);
        await _brands.UpdateAsync(brand, cancellationToken);

        var productCount = await _products.CountAsync(
            product => product.BrandId == brand.Id && product.IsActive, cancellationToken);

        return brand.ToDetail(productCount);
    }

    public async Task DeleteBrandAsync(string id, CancellationToken cancellationToken = default)
    {
        var brand = await _brands.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Brand", id);

        // Refuse rather than orphan products. Deactivating the brand is the
        // intended way to take it off the shop without breaking its catalogue.
        var productCount = await _products.CountAsync(
            product => product.BrandId == brand.Id, cancellationToken);

        if (productCount > 0)
        {
            throw new ConflictException(
                $"'{brand.Name}' still has {productCount} product(s). Delete or reassign them first, " +
                "or set the brand inactive to hide it from the shop.");
        }

        await _brands.SoftDeleteAsync(id, cancellationToken);
    }

    private async Task EnsureBrandSlugFreeAsync(string slug, string? excludeId, CancellationToken cancellationToken)
    {
        if (!await _brands.IsSlugAvailableAsync(slug, excludeId, cancellationToken))
        {
            throw new ConflictException($"The slug '{slug}' is already in use by another brand.");
        }
    }

    private static Brand Apply(Brand brand, BrandRequest request, string slug)
    {
        brand.Name = request.Name.Trim();
        brand.Slug = slug;
        brand.Tagline = request.Tagline.Trim();
        brand.Story = request.Story.Trim();
        brand.Philosophy = request.Philosophy.Trim();
        brand.TargetAudience = request.TargetAudience.Trim();
        brand.LogoUrl = request.LogoUrl.Trim();
        brand.HeroImageUrl = request.HeroImageUrl.Trim();
        brand.IsActive = request.IsActive;
        brand.DisplayOrder = request.DisplayOrder;
        brand.SeoTitle = request.SeoTitle?.Trim();
        brand.SeoDescription = request.SeoDescription?.Trim();
        return brand;
    }

    // ---- Categories --------------------------------------------------------

    public async Task<IReadOnlyList<CategoryDto>> ListCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var all = await _categories.ListAsync(cancellationToken: cancellationToken);
        return [.. all.OrderBy(category => category.DisplayOrder).Select(category => category.ToDto())];
    }

    public async Task<CategoryDto> CreateCategoryAsync(
        CategoryRequest request, CancellationToken cancellationToken = default)
    {
        var slug = Normalise(request.Slug);

        if (!await _categories.IsSlugAvailableAsync(slug, null, cancellationToken))
        {
            throw new ConflictException($"The slug '{slug}' is already in use by another category.");
        }

        await EnsureParentExistsAsync(request.ParentCategoryId, null, cancellationToken);

        var category = Apply(new Category(), request, slug);
        return (await _categories.AddAsync(category, cancellationToken)).ToDto();
    }

    public async Task<CategoryDto> UpdateCategoryAsync(
        string id, CategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await _categories.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Category", id);

        var slug = Normalise(request.Slug);

        if (!await _categories.IsSlugAvailableAsync(slug, id, cancellationToken))
        {
            throw new ConflictException($"The slug '{slug}' is already in use by another category.");
        }

        await EnsureParentExistsAsync(request.ParentCategoryId, id, cancellationToken);

        Apply(category, request, slug);
        await _categories.UpdateAsync(category, cancellationToken);

        return category.ToDto();
    }

    public async Task DeleteCategoryAsync(string id, CancellationToken cancellationToken = default)
    {
        _ = await _categories.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Category", id);

        var children = await _categories.CountAsync(
            category => category.ParentCategoryId == id, cancellationToken);

        if (children > 0)
        {
            throw new ConflictException("Delete or reassign the child categories first.");
        }

        await _categories.SoftDeleteAsync(id, cancellationToken);
    }

    private async Task EnsureParentExistsAsync(
        string? parentId, string? selfId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(parentId))
        {
            return;
        }

        if (parentId == selfId)
        {
            throw new ConflictException("A category cannot be its own parent.");
        }

        var parent = await _categories.GetByIdAsync(parentId, cancellationToken)
            ?? throw new NotFoundException("Parent category", parentId);

        // One level of nesting only, as the data model specifies.
        if (parent.ParentCategoryId is not null)
        {
            throw new ConflictException("Categories support only one level of nesting.");
        }
    }

    private static Category Apply(Category category, CategoryRequest request, string slug)
    {
        category.Name = request.Name.Trim();
        category.Slug = slug;
        category.ParentCategoryId = string.IsNullOrWhiteSpace(request.ParentCategoryId)
            ? null
            : request.ParentCategoryId;
        category.ImageUrl = request.ImageUrl.Trim();
        category.IsActive = request.IsActive;
        category.DisplayOrder = request.DisplayOrder;
        return category;
    }

    // ---- Products ----------------------------------------------------------

    public async Task<PagedResponse<ProductDetailDto>> ListProductsAsync(
        PageRequest request, CancellationToken cancellationToken = default)
    {
        var page = await _products.PageAsync(request.Page, request.PageSize, null, cancellationToken);
        return PagedResponse<ProductDetailDto>.From(page, product => product.ToDetail());
    }

    public async Task<ProductDetailDto> GetProductAsync(string id, CancellationToken cancellationToken = default) =>
        (await LoadProductAsync(id, cancellationToken)).ToDetail();

    public async Task<ProductDetailDto> CreateProductAsync(
        ProductRequest request, CancellationToken cancellationToken = default)
    {
        var slug = Normalise(request.Slug);

        if (!await _products.IsSlugAvailableAsync(slug, null, cancellationToken))
        {
            throw new ConflictException($"The slug '{slug}' is already in use by another product.");
        }

        await ValidateReferencesAsync(request, cancellationToken);

        // A new product starts with no variants; they are added through the
        // variant endpoints, which own stock.
        var product = Apply(new Product(), request, slug);
        return (await _products.AddAsync(product, cancellationToken)).ToDetail();
    }

    public async Task<ProductDetailDto> UpdateProductAsync(
        string id, ProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await LoadProductAsync(id, cancellationToken);
        var slug = Normalise(request.Slug);

        if (!await _products.IsSlugAvailableAsync(slug, id, cancellationToken))
        {
            throw new ConflictException($"The slug '{slug}' is already in use by another product.");
        }

        await ValidateReferencesAsync(request, cancellationToken);

        Apply(product, request, slug);
        await _products.UpdateAsync(product, cancellationToken);

        return product.ToDetail();
    }

    public async Task DeleteProductAsync(string id, CancellationToken cancellationToken = default)
    {
        _ = await LoadProductAsync(id, cancellationToken);
        await _products.SoftDeleteAsync(id, cancellationToken);
    }

    private async Task ValidateReferencesAsync(ProductRequest request, CancellationToken cancellationToken)
    {
        var brand = await _brands.GetByIdAsync(request.BrandId, cancellationToken)
            ?? throw new NotFoundException("Brand", request.BrandId);

        _ = brand;

        foreach (var categoryId in request.CategoryIds)
        {
            _ = await _categories.GetByIdAsync(categoryId, cancellationToken)
                ?? throw new NotFoundException("Category", categoryId);
        }

        if (request.SalePrice is { } sale && sale >= request.BasePrice)
        {
            throw new ConflictException("The sale price must be lower than the base price.");
        }
    }

    private static Product Apply(Product product, ProductRequest request, string slug)
    {
        product.Name = request.Name.Trim();
        product.Slug = slug;
        product.BrandId = request.BrandId;
        product.CategoryIds = [.. request.CategoryIds];
        product.Description = request.Description.Trim();
        product.ShortDescription = request.ShortDescription.Trim();
        product.Fabric = request.Fabric.Trim();
        product.CareInstructions = request.CareInstructions.Trim();
        product.BasePrice = request.BasePrice;
        product.SalePrice = request.SalePrice;
        product.Currency = "LKR";
        product.SizeGuideId = string.IsNullOrWhiteSpace(request.SizeGuideId) ? null : request.SizeGuideId;
        product.Tags = [.. request.Tags];
        product.IsFeatured = request.IsFeatured;
        product.IsNewArrival = request.IsNewArrival;
        product.IsActive = request.IsActive;
        product.SeoTitle = request.SeoTitle?.Trim();
        product.SeoDescription = request.SeoDescription?.Trim();
        product.Specifications =
        [
            .. request.Specifications.Select(specification => new Specification
            {
                Label = specification.Label.Trim(),
                Value = specification.Value.Trim()
            })
        ];
        product.Images =
        [
            .. request.Images.Select(image => new ProductImage
            {
                Url = image.Url.Trim(),
                Alt = image.Alt.Trim(),
                Order = image.Order
            })
        ];
        return product;
    }

    // ---- Variants ----------------------------------------------------------

    public async Task<ProductDetailDto> AddVariantAsync(
        string productId, VariantRequest request, CancellationToken cancellationToken = default)
    {
        var product = await LoadProductAsync(productId, cancellationToken);
        var sku = request.Sku.Trim().ToUpperInvariant();

        if (product.Variants.Any(variant => variant.Sku == sku))
        {
            throw new ConflictException($"This product already has a variant with SKU '{sku}'.");
        }

        // SKUs are unique across the whole catalogue, not just within a product.
        if (!await _products.IsSkuAvailableAsync(sku, productId, cancellationToken))
        {
            throw new ConflictException($"SKU '{sku}' is already used by another product.");
        }

        product.Variants.Add(ToVariant(request, sku));
        await _products.UpdateAsync(product, cancellationToken);

        return product.ToDetail();
    }

    public async Task<ProductDetailDto> UpdateVariantAsync(
        string productId, string sku, VariantRequest request, CancellationToken cancellationToken = default)
    {
        var product = await LoadProductAsync(productId, cancellationToken);

        var existing = product.Variants.FirstOrDefault(variant => variant.Sku == sku)
            ?? throw new NotFoundException("Variant", sku);

        var newSku = request.Sku.Trim().ToUpperInvariant();

        if (newSku != sku && !await _products.IsSkuAvailableAsync(newSku, productId, cancellationToken))
        {
            throw new ConflictException($"SKU '{newSku}' is already used by another product.");
        }

        existing.Sku = newSku;
        existing.Size = request.Size.Trim();
        existing.Color = request.Color.Trim();
        existing.ColorHex = request.ColorHex.Trim();
        existing.StockQuantity = request.StockQuantity;
        existing.PriceOverride = request.PriceOverride;
        existing.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
        existing.IsActive = request.IsActive;

        await _products.UpdateAsync(product, cancellationToken);
        return product.ToDetail();
    }

    public async Task<ProductDetailDto> DeleteVariantAsync(
        string productId, string sku, CancellationToken cancellationToken = default)
    {
        var product = await LoadProductAsync(productId, cancellationToken);

        var existing = product.Variants.FirstOrDefault(variant => variant.Sku == sku)
            ?? throw new NotFoundException("Variant", sku);

        // Deactivate rather than remove. Past orders reference the SKU, and
        // dropping it would break their line items.
        existing.IsActive = false;
        existing.StockQuantity = 0;

        await _products.UpdateAsync(product, cancellationToken);
        return product.ToDetail();
    }

    private static ProductVariant ToVariant(VariantRequest request, string sku) => new()
    {
        Sku = sku,
        Size = request.Size.Trim(),
        Color = request.Color.Trim(),
        ColorHex = request.ColorHex.Trim(),
        StockQuantity = request.StockQuantity,
        PriceOverride = request.PriceOverride,
        ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
        IsActive = request.IsActive
    };

    private async Task<Product> LoadProductAsync(string id, CancellationToken cancellationToken) =>
        await _products.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Product", id);

    private static string Normalise(string slug) => slug.Trim().ToLowerInvariant();
}
