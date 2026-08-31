using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MSTailors.Application.Common.Dtos;
using MSTailors.Application.Customers;
using MSTailors.Application.Customers.Dtos;
using MSTailors.Application.Orders.Dtos;

namespace MSTailors.Api.Controllers;

/// <summary>
/// The signed-in customer's own data. Every action derives the customer id from
/// the validated JWT — there is no route or body parameter that could be used
/// to read someone else's record.
/// </summary>
[Route("api/me")]
[Authorize]
public sealed class MeController : ApiControllerBase
{
    private readonly ICustomerService _customers;

    public MeController(ICustomerService customers) => _customers = customers;

    private string CustomerId => CurrentCustomerId!;

    [HttpGet]
    [ProducesResponseType(typeof(CustomerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CustomerProfileDto>> GetProfile(CancellationToken cancellationToken)
        => Ok(await _customers.GetProfileAsync(CustomerId, cancellationToken));

    [HttpPut]
    [ProducesResponseType(typeof(CustomerProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerProfileDto>> UpdateProfile(
        [FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
        => Ok(await _customers.UpdateProfileAsync(CustomerId, request, cancellationToken));

    [HttpGet("addresses")]
    [ProducesResponseType(typeof(IReadOnlyList<AddressDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AddressDto>>> GetAddresses(CancellationToken cancellationToken)
        => Ok(await _customers.GetAddressesAsync(CustomerId, cancellationToken));

    /// <summary>Replaces the whole address book. Exactly one entry ends up default.</summary>
    [HttpPut("addresses")]
    [ProducesResponseType(typeof(IReadOnlyList<AddressDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AddressDto>>> ReplaceAddresses(
        [FromBody] List<AddressRequest> addresses, CancellationToken cancellationToken)
        => Ok(await _customers.ReplaceAddressesAsync(CustomerId, addresses, cancellationToken));

    [HttpGet("orders")]
    [ProducesResponseType(typeof(PagedResponse<OrderSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<OrderSummaryDto>>> GetOrders(
        [FromQuery] PageRequest request, CancellationToken cancellationToken)
        => Ok(await _customers.GetOrdersAsync(CustomerId, request, cancellationToken));

    /// <summary>404s for an order belonging to someone else — not 403, which would confirm it exists.</summary>
    [HttpGet("orders/{orderNumber}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetOrder(string orderNumber, CancellationToken cancellationToken)
        => Ok(await _customers.GetOrderAsync(CustomerId, orderNumber, cancellationToken));

    [HttpGet("wishlist")]
    [ProducesResponseType(typeof(IReadOnlyList<WishlistEntryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WishlistEntryDto>>> GetWishlist(CancellationToken cancellationToken)
        => Ok(await _customers.GetWishlistAsync(CustomerId, cancellationToken));

    [HttpPost("wishlist")]
    [ProducesResponseType(typeof(IReadOnlyList<WishlistEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<WishlistEntryDto>>> AddWishlistItem(
        [FromBody] AddWishlistItemRequest request, CancellationToken cancellationToken)
        => Ok(await _customers.AddWishlistItemAsync(CustomerId, request, cancellationToken));

    [HttpDelete("wishlist/{productId}")]
    [ProducesResponseType(typeof(IReadOnlyList<WishlistEntryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WishlistEntryDto>>> RemoveWishlistItem(
        string productId, CancellationToken cancellationToken)
        => Ok(await _customers.RemoveWishlistItemAsync(CustomerId, productId, cancellationToken));
}
