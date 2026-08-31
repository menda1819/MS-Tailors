using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Logging.Abstractions;
using MSTailors.Infrastructure.Persistence;
using MSTailors.Tests.Fixtures;

namespace MSTailors.Tests.Repositories;

public class IndexInitializerTests : RepositoryTestBase
{
    public IndexInitializerTests(MongoServerFixture server) : base(server)
    {
    }

    private async Task<List<BsonDocument>> IndexesOfAsync<T>(IMongoCollection<T> collection)
        => await (await collection.Indexes.ListAsync()).ToListAsync();

    [Fact]
    public async Task Running_the_initializer_twice_is_a_no_op()
    {
        RequireMongo();

        // The base class already ran it once. Running again must not throw,
        // which is what makes it safe to call on every startup.
        var initializer = new MongoIndexInitializer(Context, NullLogger<MongoIndexInitializer>.Instance);

        var act = async () => await initializer.CreateIndexesAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Unique_slug_indexes_exist_on_every_slugged_collection()
    {
        RequireMongo();

        (await IndexesOfAsync(Context.Brands)).Should()
            .Contain(index => index["name"] == "ux_brands_slug" && index.Contains("unique"));
        (await IndexesOfAsync(Context.Products)).Should()
            .Contain(index => index["name"] == "ux_products_slug" && index.Contains("unique"));
        (await IndexesOfAsync(Context.Categories)).Should()
            .Contain(index => index["name"] == "ux_categories_slug" && index.Contains("unique"));
        (await IndexesOfAsync(Context.Services)).Should()
            .Contain(index => index["name"] == "ux_services_slug" && index.Contains("unique"));
    }

    [Fact]
    public async Task Unique_indexes_exist_on_sku_email_ordernumber_and_coupon_code()
    {
        RequireMongo();

        (await IndexesOfAsync(Context.Products)).Should()
            .Contain(index => index["name"] == "ux_products_variant_sku" && index.Contains("unique"));
        (await IndexesOfAsync(Context.Customers)).Should()
            .Contain(index => index["name"] == "ux_customers_email" && index.Contains("unique"));
        (await IndexesOfAsync(Context.Orders)).Should()
            .Contain(index => index["name"] == "ux_orders_ordernumber" && index.Contains("unique"));
        (await IndexesOfAsync(Context.Coupons)).Should()
            .Contain(index => index["name"] == "ux_coupons_code" && index.Contains("unique"));
    }

    [Fact]
    public async Task Compound_product_indexes_exist()
    {
        RequireMongo();

        var indexes = await IndexesOfAsync(Context.Products);
        var names = indexes.Select(index => index["name"].AsString).ToList();

        names.Should().Contain("ix_products_brand_active");
        names.Should().Contain("ix_products_categories_active");

        var brandActive = indexes.Single(index => index["name"] == "ix_products_brand_active");
        brandActive["key"].AsBsonDocument.Names.Should().ContainInOrder("brandId", "isActive");
    }

    [Fact]
    public async Task Text_index_covers_name_description_and_tags()
    {
        RequireMongo();

        var indexes = await IndexesOfAsync(Context.Products);
        var text = indexes.Single(index => index["name"] == "tx_products_search");

        var weights = text["weights"].AsBsonDocument;
        weights.Names.Should().BeEquivalentTo(["description", "name", "tags"]);
        weights["name"].ToInt32().Should().Be(10, "a name match should outrank a description match");
    }

    [Fact]
    public async Task Carts_have_a_ttl_index_on_expiresAt()
    {
        RequireMongo();

        var indexes = await IndexesOfAsync(Context.Carts);
        var ttl = indexes.Single(index => index["name"] == "ttl_carts_expiresat");

        ttl.Contains("expireAfterSeconds").Should().BeTrue("without this MongoDB never reaps abandoned carts");
        ttl["expireAfterSeconds"].ToDouble().Should().Be(0);
        ttl["key"].AsBsonDocument.Names.Should().ContainSingle().Which.Should().Be("expiresAt");
    }

    [Fact]
    public async Task Cart_owner_indexes_are_sparse()
    {
        RequireMongo();

        var indexes = await IndexesOfAsync(Context.Carts);

        // Without sparse, every guest cart would index a null customerId.
        indexes.Single(index => index["name"] == "ix_carts_customer")
            .Contains("sparse").Should().BeTrue();
        indexes.Single(index => index["name"] == "ix_carts_anonymous")
            .Contains("sparse").Should().BeTrue();
    }

    [Fact]
    public async Task Products_without_variants_do_not_collide_on_the_sku_index()
    {
        RequireMongo();

        // The partial filter is what allows this. A plain unique index on
        // variants.sku would treat two variant-less products as duplicate nulls.
        var collection = Context.Products;
        var first = TestData.Product("no-variants-one");
        var second = TestData.Product("no-variants-two");
        first.Variants = [];
        second.Variants = [];

        await collection.InsertOneAsync(first);
        var act = async () => await collection.InsertOneAsync(second);

        await act.Should().NotThrowAsync();
    }
}
