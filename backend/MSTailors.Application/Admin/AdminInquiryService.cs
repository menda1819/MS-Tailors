using MSTailors.Application.Admin.Dtos;
using MSTailors.Application.Common.Dtos;
using MSTailors.Application.Common.Exceptions;
using MSTailors.Application.Common.Repositories;
using MSTailors.Application.Customers;
using MSTailors.Application.Customers.Dtos;
using MSTailors.Application.Orders;
using MSTailors.Application.Orders.Dtos;
using MSTailors.Domain.Entities;
using MSTailors.Domain.Enums;

namespace MSTailors.Application.Admin;

public sealed record InquiryDto(
    string Id, string Name, string Email, string Phone,
    ServiceNeeded ServiceNeeded, string ServiceNeededLabel, string Message,
    DateTime? PreferredDate, InquiryStatus Status, DateTime ReceivedAt);

public sealed class InquiryQueryRequest : PageRequest
{
    public InquiryStatus? Status { get; init; }
    public ServiceNeeded? ServiceNeeded { get; init; }
}

public sealed class UpdateInquiryStatusRequest
{
    public InquiryStatus Status { get; init; }
}

/// <summary>Everything an admin needs about one customer on a single screen.</summary>
public sealed record AdminCustomerDetailDto(
    AdminCustomerDto Customer,
    IReadOnlyList<AddressDto> Addresses,
    IReadOnlyList<OrderSummaryDto> RecentOrders,
    int OrderCount,
    decimal LifetimeValue,
    string Currency);

public interface IAdminInquiryService
{
    Task<PagedResponse<InquiryDto>> ListAsync(InquiryQueryRequest request, CancellationToken cancellationToken = default);
    Task<InquiryDto> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<InquiryDto> UpdateStatusAsync(string id, UpdateInquiryStatusRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<InquiryStatus, long>> CountsByStatusAsync(CancellationToken cancellationToken = default);
}

public sealed class AdminInquiryService : IAdminInquiryService
{
    private readonly IRepository<Inquiry> _inquiries;

    public AdminInquiryService(IRepository<Inquiry> inquiries) => _inquiries = inquiries;

    public async Task<PagedResponse<InquiryDto>> ListAsync(
        InquiryQueryRequest request, CancellationToken cancellationToken = default)
    {
        var page = (request.Status, request.ServiceNeeded) switch
        {
            ({ } status, { } service) => await _inquiries.PageAsync(request.Page, request.PageSize,
                inquiry => inquiry.Status == status && inquiry.ServiceNeeded == service, cancellationToken),
            ({ } status, null) => await _inquiries.PageAsync(request.Page, request.PageSize,
                inquiry => inquiry.Status == status, cancellationToken),
            (null, { } service) => await _inquiries.PageAsync(request.Page, request.PageSize,
                inquiry => inquiry.ServiceNeeded == service, cancellationToken),
            _ => await _inquiries.PageAsync(request.Page, request.PageSize, null, cancellationToken)
        };

        return PagedResponse<InquiryDto>.From(page, ToDto);
    }

    public async Task<InquiryDto> GetAsync(string id, CancellationToken cancellationToken = default) =>
        ToDto(await _inquiries.GetByIdAsync(id, cancellationToken)
              ?? throw new NotFoundException("Inquiry", id));

    public async Task<InquiryDto> UpdateStatusAsync(
        string id, UpdateInquiryStatusRequest request, CancellationToken cancellationToken = default)
    {
        var inquiry = await _inquiries.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Inquiry", id);

        inquiry.Status = request.Status;
        await _inquiries.UpdateAsync(inquiry, cancellationToken);

        return ToDto(inquiry);
    }

    /// <summary>Drives the unread-style badge on the inbox nav item.</summary>
    public async Task<IReadOnlyDictionary<InquiryStatus, long>> CountsByStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var counts = new Dictionary<InquiryStatus, long>();

        foreach (var status in Enum.GetValues<InquiryStatus>())
        {
            counts[status] = await _inquiries.CountAsync(
                inquiry => inquiry.Status == status, cancellationToken);
        }

        return counts;
    }

    private static InquiryDto ToDto(Inquiry inquiry) => new(
        inquiry.Id, inquiry.Name, inquiry.Email, inquiry.Phone,
        inquiry.ServiceNeeded, inquiry.ServiceNeeded.ToDisplayName(), inquiry.Message,
        inquiry.PreferredDate, inquiry.Status, inquiry.CreatedAt);
}
