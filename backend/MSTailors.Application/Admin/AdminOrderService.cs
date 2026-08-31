using MSTailors.Application.Admin.Dtos;
using MSTailors.Application.Common.Dtos;
using MSTailors.Application.Common.Exceptions;
using MSTailors.Application.Common.Repositories;
using MSTailors.Application.Customers.Dtos;
using MSTailors.Application.Customers;
using MSTailors.Application.Orders;
using MSTailors.Application.Orders.Dtos;
using MSTailors.Domain.Enums;

namespace MSTailors.Application.Admin;

public sealed class AdminOrderQueryRequest : PageRequest
{
    public OrderStatus? Status { get; init; }
    public PaymentStatus? PaymentStatus { get; init; }
    public string? Email { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
}

public interface IAdminOrderService
{
    Task<PagedResponse<OrderSummaryDto>> ListAsync(AdminOrderQueryRequest request, CancellationToken cancellationToken = default);
    Task<OrderDto> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<OrderDto> UpdateStatusAsync(string id, UpdateOrderStatusRequest request, string changedBy, CancellationToken cancellationToken = default);
}

public interface IAdminCustomerService
{
    Task<PagedResponse<AdminCustomerDto>> ListAsync(PageRequest request, CancellationToken cancellationToken = default);
}

public sealed class AdminOrderService : IAdminOrderService
{
    private readonly IOrderRepository _orders;
    private readonly Catalog.IProductRepository _products;

    public AdminOrderService(IOrderRepository orders, Catalog.IProductRepository products)
    {
        _orders = orders;
        _products = products;
    }

    public async Task<PagedResponse<OrderSummaryDto>> ListAsync(
        AdminOrderQueryRequest request, CancellationToken cancellationToken = default)
    {
        var page = await _orders.SearchAsync(new OrderQuery
        {
            OrderStatus = request.Status,
            PaymentStatus = request.PaymentStatus,
            Email = request.Email,
            PlacedFrom = request.From,
            PlacedTo = request.To,
            Page = request.Page,
            PageSize = request.PageSize
        }, cancellationToken);

        return PagedResponse<OrderSummaryDto>.From(page, order => order.ToSummary());
    }

    public async Task<OrderDto> GetAsync(string id, CancellationToken cancellationToken = default) =>
        (await _orders.GetByIdAsync(id, cancellationToken)
         ?? throw new NotFoundException("Order", id)).ToDto();

    public async Task<OrderDto> UpdateStatusAsync(
        string id, UpdateOrderStatusRequest request, string changedBy, CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Order", id);

        if (order.OrderStatus == request.Status)
        {
            return order.ToDto();
        }

        EnsureTransitionAllowed(order.OrderStatus, request.Status);

        // Cancelling or returning puts stock back. Without this, a cancelled
        // order permanently removes inventory that was never sold.
        if (request.Status is OrderStatus.Cancelled or OrderStatus.Returned
            && order.OrderStatus is not (OrderStatus.Cancelled or OrderStatus.Returned))
        {
            foreach (var item in order.Items)
            {
                await _products.IncrementStockAsync(
                    item.ProductId, item.VariantSku, item.Quantity, cancellationToken);
            }
        }

        await _orders.ChangeStatusAsync(id, request.Status, changedBy, request.Note, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.TrackingNumber))
        {
            var reloaded = await _orders.GetByIdAsync(id, cancellationToken);

            if (reloaded is not null)
            {
                reloaded.TrackingNumber = request.TrackingNumber.Trim();
                await _orders.UpdateAsync(reloaded, cancellationToken);
            }
        }

        return (await _orders.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException("Order", id)).ToDto();
    }

    /// <summary>
    /// A delivered or cancelled order is terminal. Allowing it to move back to
    /// Processing would corrupt the audit trail and re-trigger stock movements.
    /// </summary>
    private static void EnsureTransitionAllowed(OrderStatus from, OrderStatus to)
    {
        if (from is OrderStatus.Cancelled or OrderStatus.Returned)
        {
            throw new BusinessRuleException($"An order that is {from} cannot change status again.");
        }

        if (from == OrderStatus.Delivered && to != OrderStatus.Returned)
        {
            throw new BusinessRuleException("A delivered order can only be marked as returned.");
        }
    }
}

public sealed class AdminCustomerService : IAdminCustomerService
{
    private readonly ICustomerRepository _customers;

    public AdminCustomerService(ICustomerRepository customers) => _customers = customers;

    public async Task<PagedResponse<AdminCustomerDto>> ListAsync(
        PageRequest request, CancellationToken cancellationToken = default)
    {
        var page = await _customers.PageAsync(request.Page, request.PageSize, null, cancellationToken);
        return PagedResponse<AdminCustomerDto>.From(page, customer => customer.ToAdminDto());
    }
}
