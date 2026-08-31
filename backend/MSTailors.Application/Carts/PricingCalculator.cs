using MSTailors.Domain.Entities;
using MSTailors.Domain.Enums;

namespace MSTailors.Application.Carts;

public sealed record PricedLine(
    Product Product,
    Domain.ValueObjects.ProductVariant Variant,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record PricingResult(
    IReadOnlyList<PricedLine> Lines,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal DeliveryFee,
    decimal Total,
    string? AppliedCouponCode,
    IReadOnlyList<string> Warnings);

/// <summary>
/// The single place order money is computed. Both the cart preview and checkout
/// call this, so a customer can never be shown one total and charged another.
/// </summary>
/// <remarks>
/// Nothing here reads a price from the client. Unit prices come from the product
/// document; the discount comes from the coupon document.
/// </remarks>
public static class PricingCalculator
{
    /// <summary>Flat island-wide delivery fee, waived above the threshold.</summary>
    public const decimal DeliveryFeeAmount = 450m;
    public const decimal FreeDeliveryThreshold = 15_000m;

    public static PricingResult Calculate(
        IReadOnlyList<(Product Product, Domain.ValueObjects.ProductVariant Variant, int Quantity)> items,
        Coupon? coupon,
        DateTime utcNow)
    {
        var warnings = new List<string>();
        var lines = new List<PricedLine>();

        foreach (var (product, variant, quantity) in items)
        {
            var unitPrice = product.PriceFor(variant);
            lines.Add(new PricedLine(product, variant, quantity, unitPrice, unitPrice * quantity));
        }

        var subtotal = lines.Sum(line => line.LineTotal);

        var (discount, appliedCode) = ResolveDiscount(coupon, subtotal, utcNow, warnings);

        var discountedSubtotal = subtotal - discount;
        var deliveryFee = discountedSubtotal >= FreeDeliveryThreshold || lines.Count == 0
            ? 0m
            : DeliveryFeeAmount;

        var total = discountedSubtotal + deliveryFee;

        return new PricingResult(
            lines,
            Round(subtotal),
            Round(discount),
            Round(deliveryFee),
            Round(total),
            appliedCode,
            warnings);
    }

    private static (decimal Discount, string? Code) ResolveDiscount(
        Coupon? coupon, decimal subtotal, DateTime utcNow, List<string> warnings)
    {
        if (coupon is null)
        {
            return (0m, null);
        }

        if (!coupon.IsRedeemableAt(utcNow))
        {
            warnings.Add($"Coupon '{coupon.Code}' is no longer valid and was not applied.");
            return (0m, null);
        }

        if (subtotal < coupon.MinOrderTotal)
        {
            warnings.Add(
                $"Coupon '{coupon.Code}' needs a subtotal of at least {coupon.MinOrderTotal:N2} and was not applied.");
            return (0m, null);
        }

        var discount = coupon.Type switch
        {
            CouponType.Percentage => subtotal * (coupon.Value / 100m),
            CouponType.FixedAmount => coupon.Value,
            _ => 0m
        };

        if (coupon.MaxDiscount is { } cap && discount > cap)
        {
            discount = cap;
        }

        // A discount can never exceed the subtotal, or delivery becomes free money.
        discount = Math.Min(discount, subtotal);

        return (discount, coupon.Code);
    }

    /// <summary>LKR to two decimal places, away from zero, so totals never drift by a cent.</summary>
    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
