using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MongoDB.Driver;
using MSTailors.Application.Carts.Dtos;
using MSTailors.Application.Orders.Dtos;
using MSTailors.Domain.Entities;
using MSTailors.Domain.Enums;
using MSTailors.Domain.ValueObjects;
using MSTailors.Tests.Fixtures;

namespace MSTailors.Tests.Integration;

/// <summary>
/// The checkout path end to end, through the real HTTP pipeline.
/// </summary>
public class CheckoutFlowTests : IntegrationTestBase
{
    private const string Sku = "IT-SHIRT-M";

    public CheckoutFlowTests(MongoServerFixture server) : base(server)
    {
    }

    private async Task<(Brand Brand, Product Product)> SeedCatalogAsync(
        decimal basePrice = 8_000m, int stock = 10, decimal? salePrice = null)
    {
        var brand = TestData.Brand("it-brand");
        brand.CreatedAt = brand.UpdatedAt = DateTime.UtcNow;
        await Context.Brands.InsertOneAsync(brand);

        var product = TestData.Product("it-shirt", brand.Id, basePrice, salePrice,
            variants: TestData.Variant(Sku, "M", "Navy", stock));
        product.CreatedAt = product.UpdatedAt = DateTime.UtcNow;
        await Context.Products.InsertOneAsync(product);

        return (brand, product);
    }

    private static CreateOrderRequest OrderRequest(int quantity = 2, string? coupon = null) => new()
    {
        Contact = new ContactRequest
        {
            FullName = "Nimal Perera", Email = "nimal@example.com", Phone = "0771234567"
        },
        ShippingAddress = new ShippingAddressRequest
        {
            Line1 = "28 Station Road", City = "Panadura", PostalCode = "12500"
        },
        PaymentMethod = PaymentMethod.CashOnDelivery,
        CouponCode = coupon,
        Items = [new OrderLineRequest { VariantSku = Sku, Quantity = quantity }]
    };

    [Fact]
    public async Task A_guest_can_place_an_order_and_stock_is_decremented()
    {
        RequireMongo();
        var (_, product) = await SeedCatalogAsync(basePrice: 8_000m, stock: 10);
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/orders", OrderRequest(quantity: 1));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var order = await response.Content.ReadFromJsonAsync<OrderDto>(Json);
        order!.OrderNumber.Should().MatchRegex(@"^MST-\d{4}-\d{4}$");
        order.Items.Should().ContainSingle();
        order.Subtotal.Should().Be(8_000m);
        order.DeliveryFee.Should().Be(450m, "8,000 is below the 15,000 free-delivery threshold");
        order.Total.Should().Be(8_450m);
        order.OrderStatus.Should().Be(OrderStatus.Confirmed, "cash on delivery confirms immediately");

        var reloaded = await Context.Products
            .Find(Builders<Product>.Filter.Eq(p => p.Id, product.Id)).FirstAsync();
        reloaded.Variants.Single().StockQuantity.Should().Be(9);
        reloaded.SalesCount.Should().Be(1);
    }

    [Fact]
    public async Task The_server_ignores_prices_and_recomputes_from_the_database()
    {
        RequireMongo();
        await SeedCatalogAsync(basePrice: 8_000m, stock: 10);
        var client = Factory.CreateClient();

        // A forged body claiming a 1-rupee unit price, alongside the real fields.
        var forged = new Dictionary<string, object?>
        {
            ["contact"] = new { fullName = "Forger", email = "forger@example.com", phone = "0771234567" },
            ["shippingAddress"] = new { line1 = "1 Fake Street", city = "Panadura" },
            ["paymentMethod"] = "CashOnDelivery",
            ["items"] = new[] { new { variantSku = Sku, quantity = 2, unitPrice = 1m, lineTotal = 2m } },
            ["subtotal"] = 2m,
            ["total"] = 2m,
            ["deliveryFee"] = 0m,
            ["discountAmount"] = 9_999m
        };

        var response = await client.PostAsJsonAsync("/api/orders", forged);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var order = await response.Content.ReadFromJsonAsync<OrderDto>(Json);
        order!.Items.Single().UnitPrice.Should().Be(8_000m, "the price comes from the product document");
        order.Subtotal.Should().Be(16_000m);
        order.DiscountAmount.Should().Be(0m, "no coupon was applied, so no discount is possible");
        order.Total.Should().Be(16_000m, "16,000 clears the free-delivery threshold");
    }

    [Fact]
    public async Task A_sale_price_is_honoured_over_the_base_price()
    {
        RequireMongo();
        await SeedCatalogAsync(basePrice: 8_000m, stock: 10, salePrice: 5_000m);
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/orders", OrderRequest(quantity: 2));
        var order = await response.Content.ReadFromJsonAsync<OrderDto>(Json);

        order!.Items.Single().UnitPrice.Should().Be(5_000m);
        order.Subtotal.Should().Be(10_000m);
    }

    [Fact]
    public async Task Free_delivery_applies_above_the_threshold()
    {
        RequireMongo();
        await SeedCatalogAsync(basePrice: 8_000m, stock: 10);
        var client = Factory.CreateClient();

        // 3 x 8,000 = 24,000, comfortably over the 15,000 threshold.
        var response = await client.PostAsJsonAsync("/api/orders", OrderRequest(quantity: 3));
        var order = await response.Content.ReadFromJsonAsync<OrderDto>(Json);

        order!.DeliveryFee.Should().Be(0m);
        order.Total.Should().Be(24_000m);
    }

    [Fact]
    public async Task Ordering_more_than_stock_is_refused_and_nothing_changes()
    {
        RequireMongo();
        var (_, product) = await SeedCatalogAsync(stock: 3);
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/orders", OrderRequest(quantity: 5));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var reloaded = await Context.Products
            .Find(Builders<Product>.Filter.Eq(p => p.Id, product.Id)).FirstAsync();
        reloaded.Variants.Single().StockQuantity.Should().Be(3, "a refused order moves no stock");

        (await Context.Orders.CountDocumentsAsync(Builders<Order>.Filter.Empty))
            .Should().Be(0);
    }

    [Fact]
    public async Task A_percentage_coupon_is_applied_and_consumed()
    {
        RequireMongo();
        await SeedCatalogAsync(basePrice: 8_000m, stock: 10);

        var coupon = new Coupon
        {
            Code = "WELCOME10",
            Type = CouponType.Percentage,
            Value = 10m,
            UsageLimit = 5,
            StartsAt = DateTime.UtcNow.AddDays(-1),
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await Context.Coupons.InsertOneAsync(coupon);

        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/orders", OrderRequest(2, "WELCOME10"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var order = await response.Content.ReadFromJsonAsync<OrderDto>(Json);
        order!.Subtotal.Should().Be(16_000m);
        order.DiscountAmount.Should().Be(1_600m);
        // 16,000 - 1,600 = 14,400, below the threshold, so delivery is charged.
        order.Total.Should().Be(14_850m);
        order.CouponCode.Should().Be("WELCOME10");

        var reloaded = await Context.Coupons
            .Find(Builders<Coupon>.Filter.Eq(c => c.Id, coupon.Id)).FirstAsync();
        reloaded.UsedCount.Should().Be(1, "a redeemed coupon must be consumed inside the transaction");
    }

    [Fact]
    public async Task An_expired_coupon_is_ignored_rather_than_applied()
    {
        RequireMongo();
        await SeedCatalogAsync(basePrice: 8_000m, stock: 10);

        await Context.Coupons.InsertOneAsync(new Coupon
        {
            Code = "EXPIRED",
            Type = CouponType.Percentage,
            Value = 50m,
            StartsAt = DateTime.UtcNow.AddDays(-30),
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/orders", OrderRequest(2, "EXPIRED"));

        var order = await response.Content.ReadFromJsonAsync<OrderDto>(Json);
        order!.DiscountAmount.Should().Be(0m);
        order.CouponCode.Should().BeNull();
    }

    [Fact]
    public async Task Invalid_contact_details_are_rejected_before_anything_is_written()
    {
        RequireMongo();
        await SeedCatalogAsync();
        var client = Factory.CreateClient();

        var request = OrderRequest();
        var bad = new CreateOrderRequest
        {
            Contact = new ContactRequest { FullName = "", Email = "not-an-email", Phone = "123" },
            ShippingAddress = request.ShippingAddress,
            PaymentMethod = request.PaymentMethod,
            Items = request.Items
        };

        var response = await client.PostAsJsonAsync("/api/orders", bad);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadAsStringAsync();
        problem.Should().Contain("contact.fullName");
        problem.Should().Contain("contact.email");

        (await Context.Orders.CountDocumentsAsync(Builders<Order>.Filter.Empty)).Should().Be(0);
    }

    [Fact]
    public async Task The_cart_flow_produces_the_same_totals_as_the_placed_order()
    {
        RequireMongo();
        await SeedCatalogAsync(basePrice: 8_000m, stock: 10);
        var client = Factory.CreateClient();

        var added = await client.PostAsJsonAsync("/api/cart/items",
            new AddCartItemRequest { VariantSku = Sku, Quantity = 1 });
        added.StatusCode.Should().Be(HttpStatusCode.OK);

        var cart = await added.Content.ReadFromJsonAsync<CartDto>(Json);
        cart!.Subtotal.Should().Be(8_000m);
        cart.Total.Should().Be(8_450m, "delivery is charged below the threshold");

        // No Items on the request: the server uses the cart it already holds.
        var placed = await client.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            Contact = new ContactRequest
            {
                FullName = "Nimal Perera", Email = "nimal@example.com", Phone = "0771234567"
            },
            ShippingAddress = new ShippingAddressRequest { Line1 = "28 Station Road", City = "Panadura" },
            PaymentMethod = PaymentMethod.CashOnDelivery
        });

        placed.StatusCode.Should().Be(HttpStatusCode.Created);

        var order = await placed.Content.ReadFromJsonAsync<OrderDto>(Json);
        order!.Total.Should().Be(cart.Total, "the customer must be charged what the cart showed");

        var afterCheckout = await client.GetAsync("/api/cart");
        var emptied = await afterCheckout.Content.ReadFromJsonAsync<CartDto>(Json);
        emptied!.Items.Should().BeEmpty("a placed order empties the basket");
    }

    [Fact]
    public async Task Order_tracking_requires_the_matching_email()
    {
        RequireMongo();
        await SeedCatalogAsync();
        var client = Factory.CreateClient();

        var placed = await client.PostAsJsonAsync("/api/orders", OrderRequest());
        var order = await placed.Content.ReadFromJsonAsync<OrderDto>(Json);

        var correct = await client.GetAsync(
            $"/api/orders/track/{order!.OrderNumber}?email=nimal@example.com");
        correct.StatusCode.Should().Be(HttpStatusCode.OK);

        var tracking = await correct.Content.ReadFromJsonAsync<OrderTrackingDto>(Json);
        tracking!.OrderNumber.Should().Be(order.OrderNumber);

        var wrong = await client.GetAsync(
            $"/api/orders/track/{order.OrderNumber}?email=someone-else@example.com");
        wrong.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "an order number alone must not expose an order");
    }

    [Fact]
    public async Task A_signed_in_customer_sees_the_order_in_their_history()
    {
        RequireMongo();
        await SeedCatalogAsync();

        var client = await CreateAuthenticatedClientAsync("buyer@example.com", UserRole.Customer);

        var placed = await client.PostAsJsonAsync("/api/orders", OrderRequest());
        placed.StatusCode.Should().Be(HttpStatusCode.Created);

        var order = await placed.Content.ReadFromJsonAsync<OrderDto>(Json);
        order!.CustomerId.Should().NotBeNull("a signed-in checkout must attach the customer");

        var history = await client.GetAsync("/api/me/orders");
        history.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await history.Content.ReadAsStringAsync();
        body.Should().Contain(order.OrderNumber);
    }

    [Fact]
    public async Task Concurrent_http_checkouts_never_oversell()
    {
        RequireMongo();
        var (_, product) = await SeedCatalogAsync(basePrice: 8_000m, stock: 5);

        // Ten simultaneous requests through the full HTTP pipeline for five units.
        var attempts = Enumerable.Range(0, 10).Select(index => Task.Run(async () =>
        {
            var client = Factory.CreateClient();
            var response = await client.PostAsJsonAsync("/api/orders", new CreateOrderRequest
            {
                Contact = new ContactRequest
                {
                    FullName = $"Buyer {index}",
                    Email = $"buyer{index}@example.com",
                    Phone = "0771234567"
                },
                ShippingAddress = new ShippingAddressRequest { Line1 = "1 Road", City = "Panadura" },
                PaymentMethod = PaymentMethod.CashOnDelivery,
                Items = [new OrderLineRequest { VariantSku = Sku, Quantity = 1 }]
            });

            return response.StatusCode;
        }));

        var statuses = await Task.WhenAll(attempts);
        var created = statuses.Count(status => status == HttpStatusCode.Created);

        var reloaded = await Context.Products
            .Find(Builders<Product>.Filter.Eq(p => p.Id, product.Id)).FirstAsync();
        var ordersWritten = await Context.Orders.CountDocumentsAsync(Builders<Order>.Filter.Empty);

        reloaded.Variants.Single().StockQuantity
            .Should().BeGreaterThanOrEqualTo(0, "stock must never go negative");
        created.Should().Be(5, "exactly the available stock should sell");
        reloaded.Variants.Single().StockQuantity.Should().Be(0);
        ordersWritten.Should().Be(created);
    }
}
