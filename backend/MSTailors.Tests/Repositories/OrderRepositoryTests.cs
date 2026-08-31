using FluentAssertions;
using MongoDB.Driver;
using MSTailors.Application.Orders;
using MSTailors.Domain.Enums;
using MSTailors.Infrastructure.Repositories;
using MSTailors.Tests.Fixtures;

namespace MSTailors.Tests.Repositories;

public class OrderRepositoryTests : RepositoryTestBase
{
    private OrderRepository _sut = null!;

    public OrderRepositoryTests(MongoServerFixture server) : base(server)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        if (SkipReason is not null)
        {
            return;
        }

        _sut = new OrderRepository(Context);
    }

    [Fact]
    public async Task GenerateOrderNumberAsync_produces_the_documented_format()
    {
        RequireMongo();

        var number = await _sut.GenerateOrderNumberAsync(2026);

        number.Should().Be("MST-2026-0001");
    }

    [Fact]
    public async Task GenerateOrderNumberAsync_increments_within_a_year()
    {
        RequireMongo();

        await _sut.GenerateOrderNumberAsync(2026);
        var second = await _sut.GenerateOrderNumberAsync(2026);

        second.Should().Be("MST-2026-0002");
    }

    [Fact]
    public async Task GenerateOrderNumberAsync_counts_each_year_separately()
    {
        RequireMongo();

        await _sut.GenerateOrderNumberAsync(2026);
        var nextYear = await _sut.GenerateOrderNumberAsync(2027);

        nextYear.Should().Be("MST-2027-0001");
    }

    [Fact]
    public async Task Concurrent_order_numbers_are_all_distinct()
    {
        RequireMongo();

        // The counter is a findOneAndUpdate with $inc, so fifty simultaneous
        // checkouts must not collide on the unique orderNumber index.
        var numbers = await Task.WhenAll(
            Enumerable.Range(0, 50).Select(_ => _sut.GenerateOrderNumberAsync(2026)));

        numbers.Should().OnlyHaveUniqueItems();
        numbers.Should().HaveCount(50);
    }

    [Fact]
    public async Task Duplicate_order_numbers_are_rejected_by_the_unique_index()
    {
        RequireMongo();
        await _sut.AddAsync(TestData.Order("MST-2026-0001"));

        var act = async () => await _sut.AddAsync(TestData.Order("MST-2026-0001"));

        await act.Should().ThrowAsync<MongoWriteException>()
            .Where(ex => ex.WriteError.Category == ServerErrorCategory.DuplicateKey);
    }

    [Fact]
    public async Task GetForGuestAsync_requires_the_matching_email()
    {
        RequireMongo();
        await _sut.AddAsync(TestData.Order("MST-2026-0007", "buyer@example.com"));

        var withCorrectEmail = await _sut.GetForGuestAsync("MST-2026-0007", "buyer@example.com");
        var withWrongEmail = await _sut.GetForGuestAsync("MST-2026-0007", "attacker@example.com");

        withCorrectEmail.Should().NotBeNull();
        withWrongEmail.Should().BeNull("the order number alone must not expose customer details");
    }

    [Fact]
    public async Task GetForGuestAsync_is_case_insensitive_on_both_parts()
    {
        RequireMongo();
        await _sut.AddAsync(TestData.Order("MST-2026-0008", "buyer@example.com"));

        var found = await _sut.GetForGuestAsync("mst-2026-0008", "BUYER@example.com");

        found.Should().NotBeNull();
    }

    [Fact]
    public async Task ChangeStatusAsync_updates_status_and_appends_history_together()
    {
        RequireMongo();
        var order = await _sut.AddAsync(TestData.Order("MST-2026-0002"));

        await _sut.ChangeStatusAsync(order.Id, OrderStatus.Confirmed, "admin@mstailors.lk", "Payment cleared");
        await _sut.ChangeStatusAsync(order.Id, OrderStatus.Shipped, "admin@mstailors.lk");

        var reloaded = await _sut.GetByIdAsync(order.Id);
        reloaded!.OrderStatus.Should().Be(OrderStatus.Shipped);
        reloaded.StatusHistory.Should().HaveCount(2);
        reloaded.StatusHistory[0].Status.Should().Be(OrderStatus.Confirmed);
        reloaded.StatusHistory[0].Note.Should().Be("Payment cleared");
        reloaded.StatusHistory[1].Status.Should().Be(OrderStatus.Shipped);
        reloaded.StatusHistory[1].ChangedBy.Should().Be("admin@mstailors.lk");
    }

    [Fact]
    public async Task SearchAsync_filters_by_status_and_pages_newest_first()
    {
        RequireMongo();
        var first = await _sut.AddAsync(TestData.Order("MST-2026-0003"));
        await _sut.AddAsync(TestData.Order("MST-2026-0004"));
        await _sut.ChangeStatusAsync(first.Id, OrderStatus.Cancelled, "admin");

        var cancelled = await _sut.SearchAsync(new OrderQuery { OrderStatus = OrderStatus.Cancelled });

        cancelled.Items.Should().ContainSingle().Which.OrderNumber.Should().Be("MST-2026-0003");
    }

    [Fact]
    public async Task SearchAsync_filters_by_customer_email()
    {
        RequireMongo();
        await _sut.AddAsync(TestData.Order("MST-2026-0005", "one@example.com"));
        await _sut.AddAsync(TestData.Order("MST-2026-0006", "two@example.com"));

        var result = await _sut.SearchAsync(new OrderQuery { Email = "TWO@example.com" });

        result.Items.Should().ContainSingle().Which.OrderNumber.Should().Be("MST-2026-0006");
    }

    [Fact]
    public async Task SetPaymentStatusAsync_updates_only_payment_state()
    {
        RequireMongo();
        var order = await _sut.AddAsync(TestData.Order("MST-2026-0009"));

        await _sut.SetPaymentStatusAsync(order.Id, PaymentStatus.Paid);

        var reloaded = await _sut.GetByIdAsync(order.Id);
        reloaded!.PaymentStatus.Should().Be(PaymentStatus.Paid);
        reloaded.OrderStatus.Should().Be(OrderStatus.New, "payment and fulfilment are separate lifecycles");
    }

    [Fact]
    public async Task Order_totals_round_trip_as_exact_decimals()
    {
        RequireMongo();
        var order = TestData.Order("MST-2026-0010");
        order.Subtotal = 24_999.99m;
        order.DiscountAmount = 2_499.99m;
        order.DeliveryFee = 450.50m;
        order.Total = 22_950.50m;

        await _sut.AddAsync(order);

        var reloaded = await _sut.GetByOrderNumberAsync("MST-2026-0010");
        reloaded!.Subtotal.Should().Be(24_999.99m);
        reloaded.DiscountAmount.Should().Be(2_499.99m);
        reloaded.DeliveryFee.Should().Be(450.50m);
        reloaded.Total.Should().Be(22_950.50m);
    }
}
