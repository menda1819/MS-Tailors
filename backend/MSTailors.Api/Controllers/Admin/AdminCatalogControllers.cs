using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MSTailors.Application.Admin;
using MSTailors.Application.Admin.Dtos;
using MSTailors.Application.Catalog.Dtos;
using MSTailors.Application.Common.Dtos;

namespace MSTailors.Api.Controllers.Admin;

/// <summary>
/// Base for every admin endpoint. The authorization policy is declared once
/// here so a new admin controller cannot be added without it.
/// </summary>
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("api/admin")]
public abstract class AdminControllerBase : ApiControllerBase
{
}

[Route("api/admin/brands")]
public sealed class AdminBrandsController : AdminControllerBase
{
    private readonly IAdminCatalogService _catalog;

    public AdminBrandsController(IAdminCatalogService catalog) => _catalog = catalog;

    [HttpGet]
    public async Task<ActionResult<PagedResponse<BrandDetailDto>>> List(
        [FromQuery] PageRequest request, CancellationToken cancellationToken)
        => Ok(await _catalog.ListBrandsAsync(request, cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(BrandDetailDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<BrandDetailDto>> Create(
        [FromBody] BrandRequest request, CancellationToken cancellationToken)
    {
        var created = await _catalog.CreateBrandAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<BrandDetailDto>> Update(
        string id, [FromBody] BrandRequest request, CancellationToken cancellationToken)
        => Ok(await _catalog.UpdateBrandAsync(id, request, cancellationToken));

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _catalog.DeleteBrandAsync(id, cancellationToken);
        return NoContent();
    }
}

[Route("api/admin/categories")]
public sealed class AdminCategoriesController : AdminControllerBase
{
    private readonly IAdminCatalogService _catalog;

    public AdminCategoriesController(IAdminCatalogService catalog) => _catalog = catalog;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> List(CancellationToken cancellationToken)
        => Ok(await _catalog.ListCategoriesAsync(cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CategoryDto>> Create(
        [FromBody] CategoryRequest request, CancellationToken cancellationToken)
    {
        var created = await _catalog.CreateCategoryAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CategoryDto>> Update(
        string id, [FromBody] CategoryRequest request, CancellationToken cancellationToken)
        => Ok(await _catalog.UpdateCategoryAsync(id, request, cancellationToken));

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _catalog.DeleteCategoryAsync(id, cancellationToken);
        return NoContent();
    }
}

[Route("api/admin/products")]
public sealed class AdminProductsController : AdminControllerBase
{
    private readonly IAdminCatalogService _catalog;

    public AdminProductsController(IAdminCatalogService catalog) => _catalog = catalog;

    [HttpGet]
    public async Task<ActionResult<PagedResponse<ProductDetailDto>>> List(
        [FromQuery] PageRequest request, CancellationToken cancellationToken)
        => Ok(await _catalog.ListProductsAsync(request, cancellationToken));

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDetailDto>> Get(string id, CancellationToken cancellationToken)
        => Ok(await _catalog.GetProductAsync(id, cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductDetailDto>> Create(
        [FromBody] ProductRequest request, CancellationToken cancellationToken)
    {
        var created = await _catalog.CreateProductAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProductDetailDto>> Update(
        string id, [FromBody] ProductRequest request, CancellationToken cancellationToken)
        => Ok(await _catalog.UpdateProductAsync(id, request, cancellationToken));

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _catalog.DeleteProductAsync(id, cancellationToken);
        return NoContent();
    }

    // ---- Variants ----------------------------------------------------------

    /// <summary>
    /// Variants have their own endpoints so editing a description can never
    /// overwrite live stock.
    /// </summary>
    [HttpPost("{id}/variants")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDetailDto>> AddVariant(
        string id, [FromBody] VariantRequest request, CancellationToken cancellationToken)
    {
        var updated = await _catalog.AddVariantAsync(id, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, updated);
    }

    [HttpPut("{id}/variants/{sku}")]
    public async Task<ActionResult<ProductDetailDto>> UpdateVariant(
        string id, string sku, [FromBody] VariantRequest request, CancellationToken cancellationToken)
        => Ok(await _catalog.UpdateVariantAsync(id, sku, request, cancellationToken));

    /// <summary>Deactivates the variant. Past orders still reference its SKU.</summary>
    [HttpDelete("{id}/variants/{sku}")]
    public async Task<ActionResult<ProductDetailDto>> DeleteVariant(
        string id, string sku, CancellationToken cancellationToken)
        => Ok(await _catalog.DeleteVariantAsync(id, sku, cancellationToken));
}
