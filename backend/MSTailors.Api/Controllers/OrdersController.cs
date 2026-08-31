using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MSTailors.Application.Orders;
using MSTailors.Application.Orders.Dtos;

namespace MSTailors.Api.Controllers;

[Route("api/orders")]
public sealed class OrdersController : ApiControllerBase
{
    private readonly ICheckoutService _checkout;

    public OrdersController(ICheckoutService checkout) => _checkout = checkout;

    /// <summary>
    /// Places an order. The server recomputes every price from the database,
    /// re-validates stock, decrements variant stock atomically inside a
    /// transaction, allocates the order number, and rolls back on any failure.
    /// No price the client sends is trusted, or even accepted.
    /// </summary>
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Checkout)]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderDto>> Place(
        [FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await _checkout.PlaceOrderAsync(ResolveCartOwner(), request, cancellationToken);

        return CreatedAtAction(
            nameof(Track),
            new { orderNumber = order.OrderNumber, email = order.Contact.Email },
            order);
    }

    /// <summary>
    /// Public order tracking. Requires both the order number and the email it
    /// was placed with — order numbers are sequential and therefore guessable.
    /// </summary>
    [HttpGet("track/{orderNumber}")]
    [ProducesResponseType(typeof(OrderTrackingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderTrackingDto>> Track(
        string orderNumber, [FromQuery] string email, CancellationToken cancellationToken)
        => Ok(await _checkout.TrackAsync(orderNumber, email ?? string.Empty, cancellationToken));
}
