using Microsoft.AspNetCore.Mvc;
using MSTailors.Application.Carts;
using MSTailors.Application.Carts.Dtos;

namespace MSTailors.Api.Controllers;

/// <summary>
/// The cart works for anonymous visitors via a cookie id and for signed-in
/// customers via their JWT. Every response is priced server-side.
/// </summary>
[Route("api/cart")]
public sealed class CartController : ApiControllerBase
{
    private readonly ICartService _cart;

    public CartController(ICartService cart) => _cart = cart;

    [HttpGet]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CartDto>> Get(CancellationToken cancellationToken)
        => Ok(await _cart.GetAsync(ResolveCartOwner(), cancellationToken));

    /// <summary>Adds a variant, or increases its quantity if already present.</summary>
    [HttpPost("items")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CartDto>> AddItem(
        [FromBody] AddCartItemRequest request, CancellationToken cancellationToken)
        => Ok(await _cart.AddItemAsync(ResolveCartOwner(), request, cancellationToken));

    /// <summary>Sets an exact quantity. Zero removes the line.</summary>
    [HttpPut("items/{sku}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> UpdateItem(
        string sku, [FromBody] UpdateCartItemRequest request, CancellationToken cancellationToken)
        => Ok(await _cart.UpdateItemAsync(ResolveCartOwner(), sku, request, cancellationToken));

    [HttpDelete("items/{sku}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> RemoveItem(string sku, CancellationToken cancellationToken)
        => Ok(await _cart.RemoveItemAsync(ResolveCartOwner(), sku, cancellationToken));

    /// <summary>Empties the cart. The cart itself survives.</summary>
    [HttpDelete]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CartDto>> Clear(CancellationToken cancellationToken)
        => Ok(await _cart.ClearAsync(ResolveCartOwner(), cancellationToken));

    [HttpPost("coupon")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CartDto>> ApplyCoupon(
        [FromBody] ApplyCouponRequest request, CancellationToken cancellationToken)
        => Ok(await _cart.ApplyCouponAsync(ResolveCartOwner(), request, cancellationToken));
}
