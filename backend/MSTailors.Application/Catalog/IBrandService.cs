using MSTailors.Application.Catalog.Dtos;
using MSTailors.Application.Common.Dtos;

namespace MSTailors.Application.Catalog;

public interface IBrandService
{
    Task<PagedResponse<BrandListItemDto>> ListAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<BrandDetailDto> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
}

public interface ICategoryService
{
    /// <summary>Top-level categories, each with its children nested.</summary>
    Task<IReadOnlyList<CategoryDto>> GetTreeAsync(CancellationToken cancellationToken = default);
}

public interface IProductService
{
    Task<PagedResponse<ProductListItemDto>> SearchAsync(
        ProductQueryRequest request, CancellationToken cancellationToken = default);

    Task<ProductDetailDto> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductListItemDto>> GetRelatedAsync(
        string slug, int limit, CancellationToken cancellationToken = default);
}
