using MSTailors.Application.Admin.Dtos;
using MSTailors.Application.Catalog;
using MSTailors.Application.Common.Repositories;
using MSTailors.Application.Orders;
using MSTailors.Domain.Entities;
using MSTailors.Domain.Enums;

namespace MSTailors.Application.Admin;

public interface IAdminDashboardService
{
    Task<DashboardDto> GetAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LowStockVariantDto>> GetLowStockAsync(int threshold, CancellationToken cancellationToken = default);
}

public sealed class AdminDashboardService : IAdminDashboardService
{
    /// <summary>At or below this many units, a variant is reported as low stock.</summary>
    public const int DefaultLowStockThreshold = 5;

    private readonly IOrderRepository _orders;
    private readonly IProductRepository _products;
    private readonly ICustomerRepository _customers;
    private readonly ISlugRepository<Brand> _brands;
    private readonly IRepository<Inquiry> _inquiries;

    public AdminDashboardService(
        IOrderRepository orders,
        IProductRepository products,
        ICustomerRepository customers,
        ISlugRepository<Brand> brands,
        IRepository<Inquiry> inquiries)
    {
        _orders = orders;
        _products = products;
        _customers = customers;
        _brands = brands;
        _inquiries = inquiries;
    }

    public async Task<DashboardDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var startOfDay = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Cancelled and returned orders are excluded from revenue — counting
        // them would overstate sales on every dashboard.
        var revenueStatuses = new[]
        {
            OrderStatus.Confirmed, OrderStatus.Processing, OrderStatus.Packed,
            OrderStatus.Shipped, OrderStatus.Delivered
        };

        var allOrders = await _orders.ListAsync(cancellationToken: cancellationToken);
        var revenueOrders = allOrders.Where(order => revenueStatuses.Contains(order.OrderStatus)).ToList();

        var sales = new SalesTotalsDto(
            Total: revenueOrders.Sum(order => order.Total),
            Today: revenueOrders.Where(order => order.CreatedAt >= startOfDay).Sum(order => order.Total),
            Month: revenueOrders.Where(order => order.CreatedAt >= startOfMonth).Sum(order => order.Total),
            Currency: "LKR");

        var ordersByStatus = Enum.GetValues<OrderStatus>()
            .Select(status => new OrderStatusCountDto(
                status, allOrders.Count(order => order.OrderStatus == status)))
            .ToList();

        var customerCount = await _customers.CountAsync(
            customer => customer.Role == UserRole.Customer, cancellationToken);

        var newInquiries = await _inquiries.CountAsync(
            inquiry => inquiry.Status == InquiryStatus.New, cancellationToken);

        var lowStock = await GetLowStockAsync(DefaultLowStockThreshold, cancellationToken);

        var bestSellingProducts = await _products.GetBestSellersAsync(10, cancellationToken);
        var bestSellers = bestSellingProducts
            .Where(product => product.SalesCount > 0)
            .Select(product => new BestSellerDto(
                product.Id, product.Name, product.Slug, product.SalesCount, product.EffectivePrice))
            .ToList();

        var brandPerformance = await BuildBrandPerformanceAsync(revenueOrders, cancellationToken);

        return new DashboardDto(
            sales,
            allOrders.Count,
            allOrders.Count(order => order.CreatedAt >= startOfDay),
            ordersByStatus,
            customerCount,
            newInquiries,
            lowStock,
            bestSellers,
            brandPerformance,
            now);
    }

    public async Task<IReadOnlyList<LowStockVariantDto>> GetLowStockAsync(
        int threshold, CancellationToken cancellationToken = default)
    {
        var safeThreshold = Math.Clamp(threshold, 0, 1000);
        var products = await _products.GetLowStockAsync(safeThreshold, cancellationToken);

        return
        [
            .. products
                .SelectMany(product => product.Variants
                    .Where(variant => variant.IsActive && variant.StockQuantity <= safeThreshold)
                    .Select(variant => new LowStockVariantDto(
                        product.Id, product.Name, product.Slug,
                        variant.Sku, variant.Size, variant.Color, variant.StockQuantity)))
                .OrderBy(variant => variant.StockQuantity)
                .ThenBy(variant => variant.ProductName)
        ];
    }

    private async Task<IReadOnlyList<BrandPerformanceDto>> BuildBrandPerformanceAsync(
        IReadOnlyList<Order> revenueOrders, CancellationToken cancellationToken)
    {
        // Revenue is attributed from the order line snapshots, so a brand's
        // historical performance survives a product being repriced or deleted.
        var byBrand = revenueOrders
            .SelectMany(order => order.Items)
            .GroupBy(item => item.BrandId)
            .ToDictionary(
                group => group.Key,
                group => (Units: group.Sum(item => (long)item.Quantity),
                          Revenue: group.Sum(item => item.LineTotal)));

        var brands = await _brands.ListAsync(cancellationToken: cancellationToken);
        var performance = new List<BrandPerformanceDto>();

        foreach (var brand in brands)
        {
            var stats = byBrand.TryGetValue(brand.Id, out var found) ? found : (Units: 0L, Revenue: 0m);
            var productCount = await _products.CountAsync(
                product => product.BrandId == brand.Id && product.IsActive, cancellationToken);

            performance.Add(new BrandPerformanceDto(
                brand.Id, brand.Name, brand.Slug, stats.Units, stats.Revenue, productCount));
        }

        return [.. performance.OrderByDescending(brand => brand.Revenue)];
    }
}
