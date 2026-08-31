namespace MSTailors.Application.Carts.Dtos;

public sealed record CartLineDto(
    string ProductId,
    string ProductName,
    string ProductSlug,
    string VariantSku,
    string Size,
    string Color,
    string? ImageUrl,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    int AvailableStock,
    bool InStock,
    bool PriceChanged);

/// <summary>
/// Every monetary field is computed server-side from current product data. The
/// stored unitPriceSnapshot is used only to flag PriceChanged.
/// </summary>
public sealed record CartDto(
    string Id,
    IReadOnlyList<CartLineDto> Items,
    string? CouponCode,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal DeliveryFee,
    decimal Total,
    string Currency,
    int ItemCount,
    IReadOnlyList<string> Warnings);

public sealed class AddCartItemRequest
{
    public string VariantSku { get; init; } = string.Empty;
    public int Quantity { get; init; } = 1;
}

public sealed class UpdateCartItemRequest
{
    public int Quantity { get; init; }
}

public sealed class ApplyCouponRequest
{
    public string Code { get; init; } = string.Empty;
}
