using FluentAssertions;
using MongoDB.Driver;
using MSTailors.Application.Catalog;
using MSTailors.Domain.Entities;
using MSTailors.Infrastructure.Repositories;
using MSTailors.Tests.Fixtures;

namespace MSTailors.Tests.Repositories;

public class ProductRepositoryTests : RepositoryTestBase
{
    private ProductRepository _sut = null!;
    private const string BrandA = "507f1f77bcf86cd799439011";
    private const string BrandB = "507f1f77bcf86cd799439012";

    public ProductRepositoryTests(MongoServerFixture server) : base(server)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        if (SkipReason is not null)
        {
            return;
        }

        _sut = new ProductRepository(Context);
    }

    // ---- slug and sku lookup ------------------------------------------------

    [Fact]
    public async Task GetBySlugAsync_finds_the_product()
    {
        RequireMongo();
        await _sut.AddAsync(TestData.Product("meridian-shirt", BrandA));

        var found = await _sut.GetBySlugAsync("meridian-shirt");

        found.Should().NotBeNull();
        found!.Slug.Should().Be("meridian-shirt");
    }

    [Fact]
    public async Task GetBySlugAsync_ignores_soft_deleted_products()
    {
        RequireMongo();
        var product = await _sut.AddAsync(TestData.Product("gone-shirt", BrandA));
        await _sut.SoftDeleteAsync(product.Id);

        (await _sut.GetBySlugAsync("gone-shirt")).Should().BeNull();
    }

    [Fact]
    public async Task Duplicate_slugs_are_rejected_by_the_unique_index()
    {
        RequireMongo();
        await _sut.AddAsync(TestData.Product("only-once", BrandA));

        var act = async () => await _sut.AddAsync(TestData.Product("only-once", BrandB));

        await act.Should().ThrowAsync<MongoWriteException>()
            .Where(ex => ex.WriteError.Category == ServerErrorCategory.DuplicateKey);
    }

    [Fact]
    public async Task Duplicate_variant_skus_are_rejected_across_different_products()
    {
        RequireMongo();
        await _sut.AddAsync(TestData.Product("first", BrandA, variants: TestData.Variant("SHARED-SKU")));

        var act = async () => await _sut.AddAsync(
            TestData.Product("second", BrandB, variants: TestData.Variant("SHARED-SKU")));

        await act.Should().ThrowAsync<MongoWriteException>()
            .Where(ex => ex.WriteError.Category == ServerErrorCategory.DuplicateKey);
    }

    [Fact]
    public async Task GetByVariantSkuAsync_finds_the_owning_product()
    {
        RequireMongo();
        await _sut.AddAsync(TestData.Product("multi", BrandA,
            variants: [TestData.Variant("MULTI-S", "S"), TestData.Variant("MULTI-L", "L")]));

        var found = await _sut.GetByVariantSkuAsync("MULTI-L");

        found.Should().NotBeNull();
        found!.Slug.Should().Be("multi");
    }

    [Fact]
    public async Task IsSkuAvailableAsync_is_false_for_a_taken_sku()
    {
        RequireMongo();
        await _sut.AddAsync(TestData.Product("taken", BrandA, variants: TestData.Variant("TAKEN-SKU")));

        (await _sut.IsSkuAvailableAsync("TAKEN-SKU")).Should().BeFalse();
        (await _sut.IsSkuAvailableAsync("FREE-SKU")).Should().BeTrue();
    }

    // ---- stock --------------------------------------------------------------

    [Fact]
    public async Task TryDecrementStockAsync_reduces_only_the_targeted_variant()
    {
        RequireMongo();
        var product = await _sut.AddAsync(TestData.Product("stocked", BrandA,
            variants: [TestData.Variant("STK-S", "S", stock: 5), TestData.Variant("STK-L", "L", stock: 5)]));

        var decremented = await _sut.TryDecrementStockAsync(product.Id, "STK-S", 3);

        decremented.Should().BeTrue();
        var reloaded = await _sut.GetByIdAsync(product.Id);
        reloaded!.Variants.Single(v => v.Sku == "STK-S").StockQuantity.Should().Be(2);
        reloaded.Variants.Single(v => v.Sku == "STK-L").StockQuantity.Should().Be(5);
    }

    [Fact]
    public async Task TryDecrementStockAsync_refuses_to_oversell()
    {
        RequireMongo();
        var product = await _sut.AddAsync(TestData.Product("scarce", BrandA,
            variants: TestData.Variant("SCARCE-M", stock: 2)));

        var decremented = await _sut.TryDecrementStockAsync(product.Id, "SCARCE-M", 3);

        decremented.Should().BeFalse();
        var reloaded = await _sut.GetByIdAsync(product.Id);
        reloaded!.Variants.Single().StockQuantity.Should().Be(2, "a failed decrement must not partially apply");
    }

    [Fact]
    public async Task Concurrent_decrements_cannot_drive_stock_negative()
    {
        RequireMongo();
        var product = await _sut.AddAsync(TestData.Product("contested", BrandA,
            variants: TestData.Variant("CONTESTED-M", stock: 10)));

        // Twenty concurrent buyers, one item each, ten in stock.
        var attempts = Enumerable.Range(0, 20)
            .Select(_ => _sut.TryDecrementStockAsync(product.Id, "CONTESTED-M", 1));

        var results = await Task.WhenAll(attempts);

        results.Count(succeeded => succeeded).Should().Be(10);
        var reloaded = await _sut.GetByIdAsync(product.Id);
        reloaded!.Variants.Single().StockQuantity.Should().Be(0);
    }

    [Fact]
    public async Task IncrementStockAsync_returns_stock_after_a_cancellation()
    {
        RequireMongo();
        var product = await _sut.AddAsync(TestData.Product("returned", BrandA,
            variants: TestData.Variant("RET-M", stock: 1)));

        await _sut.TryDecrementStockAsync(product.Id, "RET-M", 1);
        await _sut.IncrementStockAsync(product.Id, "RET-M", 1);

        var reloaded = await _sut.GetByIdAsync(product.Id);
        reloaded!.Variants.Single().StockQuantity.Should().Be(1);
    }

    [Fact]
    public async Task IncrementViewCountAsync_does_not_touch_UpdatedAt()
    {
        RequireMongo();
        var product = await _sut.AddAsync(TestData.Product("viewed", BrandA));
        var originalUpdatedAt = product.UpdatedAt;

        await _sut.IncrementViewCountAsync(product.Id);

        var reloaded = await _sut.GetByIdAsync(product.Id);
        reloaded!.ViewCount.Should().Be(1);
        reloaded.UpdatedAt.Should().BeCloseTo(originalUpdatedAt, TimeSpan.FromMilliseconds(1),
            "a view is not a content edit");
    }

    // ---- search -------------------------------------------------------------

    [Fact]
    public async Task SearchAsync_filters_by_brand()
    {
        RequireMongo();
        await SeedCatalogAsync();

        var result = await _sut.SearchAsync(new ProductQuery { BrandId = BrandA });

        result.Items.Should().OnlyContain(product => product.BrandId == BrandA);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task SearchAsync_excludes_inactive_products()
    {
        RequireMongo();
        await _sut.AddAsync(TestData.Product("live-one", BrandA, variants: TestData.Variant("LIVE-M")));
        await _sut.AddAsync(TestData.Product("hidden-one", BrandA, isActive: false,
            variants: TestData.Variant("HIDDEN-M")));

        var result = await _sut.SearchAsync(new ProductQuery());

        result.Items.Should().ContainSingle().Which.Slug.Should().Be("live-one");
    }

    [Fact]
    public async Task SearchAsync_price_filter_uses_sale_price_when_present()
    {
        RequireMongo();
        // Base 20,000 but discounted to 5,000 — it must appear under a 6,000 ceiling.
        await _sut.AddAsync(TestData.Product("discounted", BrandA, basePrice: 20_000m, salePrice: 5_000m,
            variants: TestData.Variant("DISC-M")));
        await _sut.AddAsync(TestData.Product("full-price", BrandA, basePrice: 20_000m,
            variants: TestData.Variant("FULL-M")));

        var result = await _sut.SearchAsync(new ProductQuery { MaxPrice = 6_000m });

        result.Items.Should().ContainSingle().Which.Slug.Should().Be("discounted");
    }

    [Fact]
    public async Task SearchAsync_in_stock_only_uses_variant_stock()
    {
        RequireMongo();
        await _sut.AddAsync(TestData.Product("has-stock", BrandA,
            variants: [TestData.Variant("HS-S", "S", stock: 0), TestData.Variant("HS-L", "L", stock: 4)]));
        await _sut.AddAsync(TestData.Product("no-stock", BrandA,
            variants: [TestData.Variant("NS-S", "S", stock: 0), TestData.Variant("NS-L", "L", stock: 0)]));

        var result = await _sut.SearchAsync(new ProductQuery { InStockOnly = true });

        result.Items.Should().ContainSingle().Which.Slug.Should().Be("has-stock");
    }

    [Fact]
    public async Task SearchAsync_finds_products_by_text()
    {
        RequireMongo();
        await _sut.AddAsync(TestData.Product("linen-camp-shirt", BrandA, variants: TestData.Variant("LINEN-M")));
        await _sut.AddAsync(TestData.Product("wool-trouser", BrandA, variants: TestData.Variant("WOOL-M")));

        var result = await _sut.SearchAsync(new ProductQuery { SearchTerm = "linen" });

        result.Items.Should().ContainSingle().Which.Slug.Should().Be("linen-camp-shirt");
    }

    [Fact]
    public async Task SearchAsync_sorts_and_pages()
    {
        RequireMongo();
        for (var i = 1; i <= 5; i++)
        {
            await _sut.AddAsync(TestData.Product($"priced-{i}", BrandA, basePrice: i * 1_000m,
                variants: TestData.Variant($"PRICED-{i}")));
        }

        var page = await _sut.SearchAsync(new ProductQuery
        {
            Sort = ProductSort.PriceAscending,
            Page = 2,
            PageSize = 2
        });

        page.Items.Should().HaveCount(2);
        page.Items.Select(product => product.BasePrice).Should().ContainInOrder(3_000m, 4_000m);
        page.TotalCount.Should().Be(5);
        page.TotalPages.Should().Be(3);
        page.HasNext.Should().BeTrue();
        page.HasPrevious.Should().BeTrue();
    }

    [Fact]
    public async Task Decimal_prices_survive_a_round_trip_exactly()
    {
        RequireMongo();
        // 8,999.99 has no exact binary representation. Decimal128 must return it unchanged.
        await _sut.AddAsync(TestData.Product("precise", BrandA, basePrice: 8_999.99m));

        var reloaded = await _sut.GetBySlugAsync("precise");

        reloaded!.BasePrice.Should().Be(8_999.99m);
    }

    private async Task SeedCatalogAsync()
    {
        await _sut.AddAsync(TestData.Product("a-one", BrandA, variants: TestData.Variant("A1")));
        await _sut.AddAsync(TestData.Product("a-two", BrandA, variants: TestData.Variant("A2")));
        await _sut.AddAsync(TestData.Product("b-one", BrandB, variants: TestData.Variant("B1")));
    }
}
