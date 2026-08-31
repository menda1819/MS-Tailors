using MSTailors.Domain.Enums;

namespace MSTailors.Application.Admin.Dtos;

public sealed record SalesTotalsDto(decimal Total, decimal Today, decimal Month, string Currency);

public sealed record OrderStatusCountDto(OrderStatus Status, long Count);

public sealed record LowStockVariantDto(
    string ProductId, string ProductName, string ProductSlug,
    string Sku, string Size, string Color, int StockQuantity);

public sealed record BestSellerDto(
    string ProductId, string ProductName, string ProductSlug, long UnitsSold, decimal Price);

public sealed record BrandPerformanceDto(
    string BrandId, string BrandName, string BrandSlug, long UnitsSold, decimal Revenue, long ProductCount);

public sealed record DashboardDto(
    SalesTotalsDto Sales,
    long TotalOrders,
    long TodayOrders,
    IReadOnlyList<OrderStatusCountDto> OrdersByStatus,
    long CustomerCount,
    long NewInquiryCount,
    IReadOnlyList<LowStockVariantDto> LowStockVariants,
    IReadOnlyList<BestSellerDto> BestSellers,
    IReadOnlyList<BrandPerformanceDto> BestPerformingBrands,
    DateTime GeneratedAtUtc);

public sealed class UpdateOrderStatusRequest
{
    public OrderStatus Status { get; init; }
    public string? Note { get; init; }
    public string? TrackingNumber { get; init; }
}
