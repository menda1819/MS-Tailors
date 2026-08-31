using FluentAssertions;
using MSTailors.Infrastructure.Repositories;
using MSTailors.Tests.Fixtures;

namespace MSTailors.Tests.Repositories;

public class CartRepositoryTests : RepositoryTestBase
{
    private CartRepository _sut = null!;
    private const string CustomerId = "507f1f77bcf86cd799439021";

    public CartRepositoryTests(MongoServerFixture server) : base(server)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        if (SkipReason is not null)
        {
            return;
        }

        _sut = new CartRepository(Context);
    }

    [Fact]
    public async Task AddItemAsync_appends_a_new_line()
    {
        RequireMongo();
        var cart = await _sut.AddAsync(TestData.Cart(anonymousId: "anon-1"));

        var updated = await _sut.AddItemAsync(cart.Id, TestData.CartItem("SKU-A", 2));

        updated.Items.Should().ContainSingle();
        updated.Items[0].VariantSku.Should().Be("SKU-A");
        updated.Items[0].Quantity.Should().Be(2);
    }

    [Fact]
    public async Task AddItemAsync_increments_an_existing_line_instead_of_duplicating()
    {
        RequireMongo();
        var cart = await _sut.AddAsync(TestData.Cart(anonymousId: "anon-2"));
        await _sut.AddItemAsync(cart.Id, TestData.CartItem("SKU-A", 2));

        var updated = await _sut.AddItemAsync(cart.Id, TestData.CartItem("SKU-A", 3));

        updated.Items.Should().ContainSingle("the same variant must not appear twice");
        updated.Items[0].Quantity.Should().Be(5);
    }

    [Fact]
    public async Task AddItemAsync_keeps_different_variants_as_separate_lines()
    {
        RequireMongo();
        var cart = await _sut.AddAsync(TestData.Cart(anonymousId: "anon-3"));

        await _sut.AddItemAsync(cart.Id, TestData.CartItem("SKU-S", 1));
        var updated = await _sut.AddItemAsync(cart.Id, TestData.CartItem("SKU-L", 1));

        updated.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddItemAsync_rejects_a_non_positive_quantity()
    {
        RequireMongo();
        var cart = await _sut.AddAsync(TestData.Cart(anonymousId: "anon-4"));

        var act = async () => await _sut.AddItemAsync(cart.Id, TestData.CartItem("SKU-A", 0));

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task SetItemQuantityAsync_updates_the_line()
    {
        RequireMongo();
        var cart = await _sut.AddAsync(TestData.Cart(anonymousId: "anon-5"));
        await _sut.AddItemAsync(cart.Id, TestData.CartItem("SKU-A", 1));

        var updated = await _sut.SetItemQuantityAsync(cart.Id, "SKU-A", 7);

        updated!.Items.Single().Quantity.Should().Be(7);
    }

    [Fact]
    public async Task SetItemQuantityAsync_removes_the_line_at_zero()
    {
        RequireMongo();
        var cart = await _sut.AddAsync(TestData.Cart(anonymousId: "anon-6"));
        await _sut.AddItemAsync(cart.Id, TestData.CartItem("SKU-A", 3));

        var updated = await _sut.SetItemQuantityAsync(cart.Id, "SKU-A", 0);

        updated!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveItemAsync_removes_only_the_named_variant()
    {
        RequireMongo();
        var cart = await _sut.AddAsync(TestData.Cart(anonymousId: "anon-7"));
        await _sut.AddItemAsync(cart.Id, TestData.CartItem("SKU-S", 1));
        await _sut.AddItemAsync(cart.Id, TestData.CartItem("SKU-L", 1));

        var updated = await _sut.RemoveItemAsync(cart.Id, "SKU-S");

        updated!.Items.Should().ContainSingle().Which.VariantSku.Should().Be("SKU-L");
    }

    [Fact]
    public async Task ClearAsync_empties_items_and_drops_the_coupon()
    {
        RequireMongo();
        var cart = TestData.Cart(anonymousId: "anon-8");
        cart.CouponCode = "WELCOME10";
        await _sut.AddAsync(cart);
        await _sut.AddItemAsync(cart.Id, TestData.CartItem("SKU-A", 1));

        await _sut.ClearAsync(cart.Id);

        var reloaded = await _sut.GetByIdAsync(cart.Id);
        reloaded!.Items.Should().BeEmpty();
        reloaded.CouponCode.Should().BeNull();
    }

    [Fact]
    public async Task MergeAnonymousIntoCustomerAsync_adopts_the_cart_when_the_customer_has_none()
    {
        RequireMongo();
        var anonymous = await _sut.AddAsync(TestData.Cart(anonymousId: "anon-9"));
        await _sut.AddItemAsync(anonymous.Id, TestData.CartItem("SKU-A", 2));

        var merged = await _sut.MergeAnonymousIntoCustomerAsync("anon-9", CustomerId);

        merged.Should().NotBeNull();
        merged!.Id.Should().Be(anonymous.Id, "the existing cart is adopted rather than copied");
        merged.CustomerId.Should().Be(CustomerId);
        merged.AnonymousId.Should().BeNull();
        merged.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task MergeAnonymousIntoCustomerAsync_sums_quantities_for_shared_variants()
    {
        RequireMongo();
        var customerCart = await _sut.AddAsync(TestData.Cart(customerId: CustomerId));
        await _sut.AddItemAsync(customerCart.Id, TestData.CartItem("SKU-A", 1));
        await _sut.AddItemAsync(customerCart.Id, TestData.CartItem("SKU-B", 1));

        var anonymous = await _sut.AddAsync(TestData.Cart(anonymousId: "anon-10"));
        await _sut.AddItemAsync(anonymous.Id, TestData.CartItem("SKU-A", 4));
        await _sut.AddItemAsync(anonymous.Id, TestData.CartItem("SKU-C", 2));

        var merged = await _sut.MergeAnonymousIntoCustomerAsync("anon-10", CustomerId);

        merged!.Id.Should().Be(customerCart.Id);
        merged.Items.Should().HaveCount(3);
        merged.Items.Single(item => item.VariantSku == "SKU-A").Quantity.Should().Be(5);
        merged.Items.Single(item => item.VariantSku == "SKU-C").Quantity.Should().Be(2);

        var abandoned = await _sut.GetByIdAsync(anonymous.Id);
        abandoned.Should().BeNull("the merged anonymous cart is soft-deleted");
    }

    [Fact]
    public async Task MergeAnonymousIntoCustomerAsync_returns_the_customer_cart_when_there_is_nothing_to_merge()
    {
        RequireMongo();
        var customerCart = await _sut.AddAsync(TestData.Cart(customerId: CustomerId));

        var merged = await _sut.MergeAnonymousIntoCustomerAsync("does-not-exist", CustomerId);

        merged!.Id.Should().Be(customerCart.Id);
    }

    [Fact]
    public async Task TouchAsync_pushes_the_expiry_forward()
    {
        RequireMongo();
        var cart = TestData.Cart(anonymousId: "anon-11");
        cart.ExpiresAt = DateTime.UtcNow.AddMinutes(5);
        await _sut.AddAsync(cart);

        await _sut.TouchAsync(cart.Id, TimeSpan.FromDays(30));

        var reloaded = await _sut.GetByIdAsync(cart.Id);
        reloaded!.ExpiresAt.Should().BeAfter(DateTime.UtcNow.AddDays(29));
    }

    [Fact]
    public async Task GetByAnonymousIdAsync_and_GetByCustomerIdAsync_find_the_right_carts()
    {
        RequireMongo();
        await _sut.AddAsync(TestData.Cart(anonymousId: "anon-12"));
        await _sut.AddAsync(TestData.Cart(customerId: CustomerId));

        (await _sut.GetByAnonymousIdAsync("anon-12")).Should().NotBeNull();
        (await _sut.GetByCustomerIdAsync(CustomerId)).Should().NotBeNull();
        (await _sut.GetByAnonymousIdAsync("nope")).Should().BeNull();
    }
}
