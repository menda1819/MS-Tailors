using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MSTailors.Application.Admin.Dtos;
using MSTailors.Domain.Enums;
using MSTailors.Tests.Fixtures;

namespace MSTailors.Tests.Integration;

/// <summary>
/// The admin authorization boundary, exercised through the real pipeline.
/// </summary>
public class AdminAuthorizationTests : IntegrationTestBase
{
    public AdminAuthorizationTests(MongoServerFixture server) : base(server)
    {
    }

    /// <summary>Every admin route, so a new one cannot quietly ship unprotected.</summary>
    public static TheoryData<string> AdminGetRoutes() => new()
    {
        "/api/admin/brands",
        "/api/admin/categories",
        "/api/admin/products",
        "/api/admin/services",
        "/api/admin/gallery",
        "/api/admin/blog",
        "/api/admin/testimonials",
        "/api/admin/coupons",
        "/api/admin/orders",
        "/api/admin/customers",
        "/api/admin/dashboard",
        "/api/admin/inventory/low-stock"
    };

    [Theory]
    [MemberData(nameof(AdminGetRoutes))]
    public async Task Anonymous_callers_are_rejected_from_every_admin_route(string route)
    {
        RequireMongo();
        var client = Factory.CreateClient();

        var response = await client.GetAsync(route);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [MemberData(nameof(AdminGetRoutes))]
    public async Task Signed_in_customers_are_forbidden_from_every_admin_route(string route)
    {
        RequireMongo();

        // Authenticated but not an admin: this must be 403, not 401 and not 200.
        var client = await CreateAuthenticatedClientAsync("shopper@example.com", UserRole.Customer);

        var response = await client.GetAsync(route);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "a customer token must never reach an admin endpoint");
    }

    [Theory]
    [MemberData(nameof(AdminGetRoutes))]
    public async Task Admins_are_allowed_through(string route)
    {
        RequireMongo();
        var client = await CreateAuthenticatedClientAsync("boss@mstailors.lk", UserRole.Admin);

        var response = await client.GetAsync(route);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Admin_writes_are_rejected_for_customers()
    {
        RequireMongo();
        var client = await CreateAuthenticatedClientAsync("shopper2@example.com", UserRole.Customer);

        var response = await client.PostAsJsonAsync("/api/admin/brands", new BrandRequest
        {
            Name = "Sneaky Brand",
            Slug = "sneaky-brand",
            Tagline = "Should never exist",
            Story = "This request must be refused before it reaches the service.",
            LogoUrl = "/logo.svg",
            HeroImageUrl = "/hero.jpg"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var count = await Context.Brands.CountDocumentsAsync(
            MongoDB.Driver.Builders<MSTailors.Domain.Entities.Brand>.Filter.Empty);
        count.Should().Be(0, "a forbidden request must not write anything");
    }

    [Fact]
    public async Task A_tampered_token_is_rejected()
    {
        RequireMongo();
        var client = await CreateAuthenticatedClientAsync("tamper@example.com", UserRole.Customer);

        // Flip the signature. Anything but a rejection means the signature is
        // not actually being validated.
        var original = client.DefaultRequestHeaders.Authorization!.Parameter!;
        var tampered = original[..^4] + (original.EndsWith("AAAA") ? "BBBB" : "AAAA");

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tampered);

        var response = await client.GetAsync("/api/admin/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_customer_cannot_read_another_customers_order()
    {
        RequireMongo();

        var victim = await Factory.CreateUserAsync("victim@example.com", "integration-test-password", UserRole.Customer);

        await Context.Orders.InsertOneAsync(new MSTailors.Domain.Entities.Order
        {
            OrderNumber = "MST-2026-9001",
            CustomerId = victim.Id,
            Contact = new MSTailors.Domain.ValueObjects.ContactDetails
            {
                FullName = "Victim", Email = "victim@example.com", Phone = "0771234567"
            },
            ShippingAddress = new MSTailors.Domain.ValueObjects.ShippingAddress
            {
                Line1 = "1 Private Road", City = "Panadura"
            },
            Total = 5_000m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var attacker = await CreateAuthenticatedClientAsync("attacker@example.com", UserRole.Customer);

        var response = await attacker.GetAsync("/api/me/orders/MST-2026-9001");

        // 404 rather than 403: confirming the order exists is itself a leak.
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Public_endpoints_stay_open()
    {
        RequireMongo();
        var client = Factory.CreateClient();

        foreach (var route in new[]
                 {
                     "/api/brands", "/api/categories", "/api/products",
                     "/api/services", "/api/gallery", "/api/testimonials", "/api/blog"
                 })
        {
            var response = await client.GetAsync(route);
            response.StatusCode.Should().Be(HttpStatusCode.OK, $"{route} is public");
        }
    }

    [Fact]
    public async Task Me_requires_authentication()
    {
        RequireMongo();
        var client = Factory.CreateClient();

        (await client.GetAsync("/api/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/api/me/orders")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/api/me/wishlist")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
