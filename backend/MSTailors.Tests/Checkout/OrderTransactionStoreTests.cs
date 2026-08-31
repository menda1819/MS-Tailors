using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using MSTailors.Application.Orders;
using MSTailors.Domain.Entities;
using MSTailors.Infrastructure.Repositories;
using MSTailors.Tests.Fixtures;

namespace MSTailors.Tests.Checkout;

/// <summary>
/// The overselling guarantee, proved against a real replica set.
/// </summary>
public class OrderTransactionStoreTests : RepositoryTestBase
{
    private OrderTransactionStore _sut = null!;
    private ProductRepository _products = null!;
    private OrderRepository _orders = null!;
    private const string BrandId = "507f1f77bcf86cd799439011";

    public OrderTransactionStoreTests(MongoServerFixture server) : base(server)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        if (SkipReason is not null)
        {
            return;
        }

        _products = new ProductRepository(Context);
        _orders = new OrderRepository(Context);
        _sut = new OrderTransactionStore(
            MongoClient, Context, NullLogger<OrderTransactionStore>.Instance);
    }

    [Fact]
    public async Task A_successful_checkout_decrements_stock_and_writes_the_order()
    {
        RequireMongo();
        var product = await _products.AddAsync(
            TestData.Product("shirt", BrandId, variants: TestData.Variant("SHIRT-M", stock: 10)));

        var outcome = await _sut.PlaceOrderAsync(
            TestData.Order(orderNumber: string.Empty),
            [new StockReservation(product.Id, "SHIRT-M", 3)],
            couponId: null);

        outcome.Succeeded.Should().BeTrue();
        outcome.Order!.OrderNumber.Should().MatchRegex(@"^MST-\d{4}-\d{4}$");

        var reloaded = await _products.GetByIdAsync(product.Id);
        reloaded!.Variants.Single().StockQuantity.Should().Be(7);
        reloaded.SalesCount.Should().Be(3);

        (await _orders.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Insufficient_stock_writes_no_order_and_moves_no_stock()
    {
        RequireMongo();
        var product = await _products.AddAsync(
            TestData.Product("scarce", BrandId, variants: TestData.Variant("SCARCE-M", stock: 2)));

        var outcome = await _sut.PlaceOrderAsync(
            TestData.Order(orderNumber: string.Empty),
            [new StockReservation(product.Id, "SCARCE-M", 5)],
            couponId: null);

        outcome.Succeeded.Should().BeFalse();
        outcome.Order.Should().BeNull();

        var reloaded = await _products.GetByIdAsync(product.Id);
        reloaded!.Variants.Single().StockQuantity.Should().Be(2);
        (await _orders.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task A_failure_on_a_later_line_rolls_back_the_earlier_lines()
    {
        RequireMongo();

        // This is what the transaction buys over the conditional update alone:
        // line one succeeds, line two cannot, and line one must be undone.
        var plentiful = await _products.AddAsync(
            TestData.Product("plentiful", BrandId, variants: TestData.Variant("PLENTY-M", stock: 50)));
        var scarce = await _products.AddAsync(
            TestData.Product("scarce", BrandId, variants: TestData.Variant("SCARCE-M", stock: 1)));

        var outcome = await _sut.PlaceOrderAsync(
            TestData.Order(orderNumber: string.Empty),
            [
                new StockReservation(plentiful.Id, "PLENTY-M", 5),
                new StockReservation(scarce.Id, "SCARCE-M", 99)
            ],
            couponId: null);

        outcome.Succeeded.Should().BeFalse();

        var reloadedPlentiful = await _products.GetByIdAsync(plentiful.Id);
        reloadedPlentiful!.Variants.Single().StockQuantity
            .Should().Be(50, "the first decrement must be rolled back by the abort");
        reloadedPlentiful.SalesCount.Should().Be(0);

        var reloadedScarce = await _products.GetByIdAsync(scarce.Id);
        reloadedScarce!.Variants.Single().StockQuantity.Should().Be(1);

        (await _orders.CountAsync()).Should().Be(0, "no order may exist for a rolled-back checkout");
    }

    [Fact]
    public async Task Twenty_simultaneous_checkouts_for_ten_units_sell_exactly_ten()
    {
        RequireMongo();

        // The headline guarantee: two simultaneous orders cannot oversell the
        // last item. Twenty buyers, one unit each, ten in stock.
        var product = await _products.AddAsync(
            TestData.Product("contested", BrandId, variants: TestData.Variant("CONTESTED-M", stock: 10)));

        var attempts = Enumerable.Range(0, 20).Select(_ => Task.Run(async () =>
        {
            try
            {
                var outcome = await _sut.PlaceOrderAsync(
                    TestData.Order(orderNumber: string.Empty),
                    [new StockReservation(product.Id, "CONTESTED-M", 1)],
                    couponId: null);

                return outcome.Succeeded;
            }
            catch (MongoException)
            {
                // A write conflict that exhausted the driver's retries is a
                // failed purchase, not a sale. Counting it as anything else
                // would hide an oversell.
                return false;
            }
        }));

        var results = await Task.WhenAll(attempts);
        var sold = results.Count(succeeded => succeeded);

        var reloaded = await _products.GetByIdAsync(product.Id);
        var remaining = reloaded!.Variants.Single().StockQuantity;
        var ordersWritten = await _orders.CountAsync();

        remaining.Should().BeGreaterThanOrEqualTo(0, "stock must never go negative");
        sold.Should().Be(10, "exactly the available stock should sell");
        remaining.Should().Be(0);
        ordersWritten.Should().Be(sold, "an order exists for every successful sale and no others");
        reloaded.SalesCount.Should().Be(10);
    }

    [Fact]
    public async Task Concurrent_checkouts_never_reuse_an_order_number()
    {
        RequireMongo();
        var product = await _products.AddAsync(
            TestData.Product("plenty", BrandId, variants: TestData.Variant("PLENTY-M", stock: 100)));

        var attempts = Enumerable.Range(0, 25).Select(_ => Task.Run(async () =>
        {
            try
            {
                var outcome = await _sut.PlaceOrderAsync(
                    TestData.Order(orderNumber: string.Empty),
                    [new StockReservation(product.Id, "PLENTY-M", 1)],
                    couponId: null);

                return outcome.Order?.OrderNumber;
            }
            catch (MongoException)
            {
                return null;
            }
        }));

        var numbers = (await Task.WhenAll(attempts)).Where(number => number is not null).ToList();

        numbers.Should().OnlyHaveUniqueItems();
        numbers.Should().NotBeEmpty();
    }

    [Fact]
    public async Task A_coupon_at_its_usage_limit_blocks_the_order()
    {
        RequireMongo();
        var product = await _products.AddAsync(
            TestData.Product("couponed", BrandId, variants: TestData.Variant("COUP-M", stock: 5)));

        var coupons = new CouponRepository(Context);
        var coupon = await coupons.AddAsync(new Coupon
        {
            Code = "SPENT",
            Type = MSTailors.Domain.Enums.CouponType.Percentage,
            Value = 10m,
            UsageLimit = 1,
            UsedCount = 1,
            StartsAt = DateTime.UtcNow.AddDays(-1),
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        });

        var outcome = await _sut.PlaceOrderAsync(
            TestData.Order(orderNumber: string.Empty),
            [new StockReservation(product.Id, "COUP-M", 1)],
            coupon.Id);

        outcome.Succeeded.Should().BeFalse();

        var reloaded = await _products.GetByIdAsync(product.Id);
        reloaded!.Variants.Single().StockQuantity
            .Should().Be(5, "the stock decrement must roll back when the coupon is refused");
        (await _orders.CountAsync()).Should().Be(0);
    }
}
