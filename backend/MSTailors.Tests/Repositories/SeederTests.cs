using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MSTailors.Domain.Entities;
using MSTailors.Domain.Enums;
using MSTailors.Infrastructure.Seeding;
using MSTailors.Tests.Fixtures;

namespace MSTailors.Tests.Repositories;

public class SeederTests : RepositoryTestBase
{
    private const string AdminPassword = "seed-password-not-a-real-secret";
    private DatabaseSeeder _sut = null!;
    private IPasswordHasher<Customer> _hasher = null!;

    public SeederTests(MongoServerFixture server) : base(server)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        if (SkipReason is not null)
        {
            return;
        }

        _hasher = new PasswordHasher<Customer>();

        var options = Options.Create(new SeedOptions
        {
            AdminEmail = "admin@mstailors.lk",
            AdminPassword = AdminPassword,
            AdminFullName = "MS Tailors Administrator",
            AdminPhone = "+94112345678"
        });

        _sut = new DatabaseSeeder(Context, options, NullLogger<DatabaseSeeder>.Instance, _hasher);
    }

    [Fact]
    public async Task Seeds_the_ten_tailoring_services()
    {
        RequireMongo();
        await _sut.SeedAsync();

        var services = await Context.Services.Find(Builders<Service>.Filter.Empty).ToListAsync();

        services.Should().HaveCount(10);
        services.Select(service => service.Slug).Should().Contain(
            ["custom-suits", "wedding-suits", "custom-shirts", "uniforms", "suit-and-coat-rental", "alterations"]);
        services.Should().OnlyContain(service => service.BulletPoints.Count > 0);
        services.Should().OnlyContain(service => service.LongDescription.Length > 200);
    }

    [Fact]
    public async Task Seeds_three_brands_with_distinct_identities()
    {
        RequireMongo();
        await _sut.SeedAsync();

        var brands = await Context.Brands.Find(Builders<Brand>.Filter.Empty).ToListAsync();

        brands.Should().HaveCount(3);
        brands.Select(brand => brand.Slug).Should().OnlyHaveUniqueItems();
        brands.Select(brand => brand.Philosophy).Should().OnlyHaveUniqueItems();
        brands.Select(brand => brand.TargetAudience).Should().OnlyHaveUniqueItems();
        brands.Should().OnlyContain(brand => brand.Story.Length > 100);
    }

    [Fact]
    public async Task Seeds_around_twenty_products_spread_across_the_brands()
    {
        RequireMongo();
        await _sut.SeedAsync();

        var products = await Context.Products.Find(Builders<Product>.Filter.Empty).ToListAsync();

        products.Should().HaveCountGreaterThanOrEqualTo(20);
        products.Select(product => product.BrandId).Distinct().Should().HaveCount(3);
        products.Should().OnlyContain(product => product.Variants.Count >= 2);
        products.Should().OnlyContain(product => product.BasePrice > 0);
        products.Should().OnlyContain(product => product.Currency == "LKR");
        products.Should().OnlyContain(product => product.Images.Count > 0);
    }

    [Fact]
    public async Task Seeded_variants_have_globally_unique_skus()
    {
        RequireMongo();
        await _sut.SeedAsync();

        var products = await Context.Products.Find(Builders<Product>.Filter.Empty).ToListAsync();
        var skus = products.SelectMany(product => product.Variants).Select(variant => variant.Sku).ToList();

        skus.Should().OnlyHaveUniqueItems();
        skus.Count.Should().BeGreaterThan(100);
    }

    [Fact]
    public async Task Seeded_products_reference_real_brands_and_categories()
    {
        RequireMongo();
        await _sut.SeedAsync();

        var brandIds = (await Context.Brands.Find(Builders<Brand>.Filter.Empty).ToListAsync())
            .Select(brand => brand.Id).ToHashSet();
        var categoryIds = (await Context.Categories.Find(Builders<Category>.Filter.Empty).ToListAsync())
            .Select(category => category.Id).ToHashSet();
        var products = await Context.Products.Find(Builders<Product>.Filter.Empty).ToListAsync();

        products.Should().OnlyContain(product => brandIds.Contains(product.BrandId));
        products.SelectMany(product => product.CategoryIds).Should().OnlyContain(id => categoryIds.Contains(id));
    }

    [Fact]
    public async Task Creates_an_admin_whose_password_comes_from_configuration()
    {
        RequireMongo();
        await _sut.SeedAsync();

        var admin = await Context.Customers
            .Find(Builders<Customer>.Filter.Eq(customer => customer.Email, "admin@mstailors.lk"))
            .FirstOrDefaultAsync();

        admin.Should().NotBeNull();
        admin.Role.Should().Be(UserRole.Admin);
        admin.PasswordHash.Should().NotBeNullOrWhiteSpace();
        admin.PasswordHash.Should().NotContain(AdminPassword, "the plaintext password must never be stored");

        _hasher.VerifyHashedPassword(admin, admin.PasswordHash, AdminPassword)
            .Should().NotBe(PasswordVerificationResult.Failed);
    }

    [Fact]
    public async Task Seeding_twice_does_not_duplicate_anything()
    {
        RequireMongo();
        await _sut.SeedAsync();
        await _sut.SeedAsync();

        (await Context.Brands.CountDocumentsAsync(Builders<Brand>.Filter.Empty)).Should().Be(3);
        (await Context.Services.CountDocumentsAsync(Builders<Service>.Filter.Empty)).Should().Be(10);
        (await Context.Customers.CountDocumentsAsync(Builders<Customer>.Filter.Empty)).Should().Be(1);
    }

    [Fact]
    public async Task Force_reseeds_without_duplicating()
    {
        RequireMongo();
        await _sut.SeedAsync();
        var firstCount = await Context.Products.CountDocumentsAsync(Builders<Product>.Filter.Empty);

        await _sut.SeedAsync(force: true);

        (await Context.Products.CountDocumentsAsync(Builders<Product>.Filter.Empty)).Should().Be(firstCount);
        (await Context.Brands.CountDocumentsAsync(Builders<Brand>.Filter.Empty)).Should().Be(3);
    }

    [Fact]
    public async Task Seeded_prices_are_realistic_lkr_amounts()
    {
        RequireMongo();
        await _sut.SeedAsync();

        var products = await Context.Products.Find(Builders<Product>.Filter.Empty).ToListAsync();

        products.Should().OnlyContain(product => product.BasePrice >= 1_000m && product.BasePrice <= 100_000m);
        products.Where(product => product.SalePrice is not null)
            .Should().OnlyContain(product => product.SalePrice < product.BasePrice,
                "a sale price above the base price would be a bug, not a discount");
    }
}
