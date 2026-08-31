using Microsoft.AspNetCore.Mvc;
using MSTailors.Application.Admin;
using MSTailors.Application.Admin.Dtos;
using MSTailors.Application.Common.Dtos;
using MSTailors.Application.Customers.Dtos;
using MSTailors.Application.Orders.Dtos;
using MSTailors.Domain.Enums;

namespace MSTailors.Api.Controllers.Admin;

[Route("api/admin/orders")]
public sealed class AdminOrdersController : AdminControllerBase
{
    private readonly IAdminOrderService _orders;

    public AdminOrdersController(IAdminOrderService orders) => _orders = orders;

    /// <summary>All orders, filterable by status, payment state, email and date range.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<OrderSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<OrderSummaryDto>>> List(
        [FromQuery] AdminOrderQueryRequest request, CancellationToken cancellationToken)
        => Ok(await _orders.ListAsync(request, cancellationToken));

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> Get(string id, CancellationToken cancellationToken)
        => Ok(await _orders.GetAsync(id, cancellationToken));

    /// <summary>
    /// Moves the order to a new status. Cancelling or returning restores stock.
    /// The acting admin is recorded in the status history.
    /// </summary>
    [HttpPut("{id}/status")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderDto>> UpdateStatus(
        string id, [FromBody] UpdateOrderStatusRequest request, CancellationToken cancellationToken)
        => Ok(await _orders.UpdateStatusAsync(id, request, CurrentUserLabel, cancellationToken));
}

[Route("api/admin/customers")]
public sealed class AdminCustomersController : AdminControllerBase
{
    private readonly IAdminCustomerService _customers;

    public AdminCustomersController(IAdminCustomerService customers) => _customers = customers;

    /// <summary>Never projects password hashes or refresh tokens.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AdminCustomerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<AdminCustomerDto>>> List(
        [FromQuery] AdminCustomerQueryRequest request, CancellationToken cancellationToken)
        => Ok(await _customers.ListAsync(request, cancellationToken));

    /// <summary>Profile, addresses, recent orders and lifetime value on one screen.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AdminCustomerDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminCustomerDetailDto>> Get(string id, CancellationToken cancellationToken)
        => Ok(await _customers.GetAsync(id, cancellationToken));
}

[Route("api/admin/inquiries")]
public sealed class AdminInquiriesController : AdminControllerBase
{
    private readonly IAdminInquiryService _inquiries;

    public AdminInquiriesController(IAdminInquiryService inquiries) => _inquiries = inquiries;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<InquiryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<InquiryDto>>> List(
        [FromQuery] InquiryQueryRequest request, CancellationToken cancellationToken)
        => Ok(await _inquiries.ListAsync(request, cancellationToken));

    /// <summary>Counts per status, for the inbox badge.</summary>
    [HttpGet("counts")]
    [ProducesResponseType(typeof(IReadOnlyDictionary<InquiryStatus, long>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyDictionary<InquiryStatus, long>>> Counts(
        CancellationToken cancellationToken)
        => Ok(await _inquiries.CountsByStatusAsync(cancellationToken));

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(InquiryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InquiryDto>> Get(string id, CancellationToken cancellationToken)
        => Ok(await _inquiries.GetAsync(id, cancellationToken));

    [HttpPut("{id}/status")]
    [ProducesResponseType(typeof(InquiryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InquiryDto>> UpdateStatus(
        string id, [FromBody] UpdateInquiryStatusRequest request, CancellationToken cancellationToken)
        => Ok(await _inquiries.UpdateStatusAsync(id, request, cancellationToken));
}

[Route("api/admin/dashboard")]
public sealed class AdminDashboardController : AdminControllerBase
{
    private readonly IAdminDashboardService _dashboard;

    public AdminDashboardController(IAdminDashboardService dashboard) => _dashboard = dashboard;

    /// <summary>
    /// Sales totals, order counts by status, customer count, low stock, best
    /// sellers and brand performance.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardDto>> Get(CancellationToken cancellationToken)
        => Ok(await _dashboard.GetAsync(cancellationToken));
}

[Route("api/admin/inventory")]
public sealed class AdminInventoryController : AdminControllerBase
{
    private readonly IAdminDashboardService _dashboard;

    public AdminInventoryController(IAdminDashboardService dashboard) => _dashboard = dashboard;

    [HttpGet("low-stock")]
    [ProducesResponseType(typeof(IReadOnlyList<LowStockVariantDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LowStockVariantDto>>> LowStock(
        [FromQuery] int threshold = AdminDashboardService.DefaultLowStockThreshold,
        CancellationToken cancellationToken = default)
        => Ok(await _dashboard.GetLowStockAsync(threshold, cancellationToken));
}
