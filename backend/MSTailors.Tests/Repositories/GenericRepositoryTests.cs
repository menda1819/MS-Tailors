using FluentAssertions;
using MongoDB.Driver;
using MSTailors.Domain.Entities;
using MSTailors.Infrastructure.Repositories;
using MSTailors.Tests.Fixtures;

namespace MSTailors.Tests.Repositories;

/// <summary>Behaviour every collection inherits, exercised through brands.</summary>
public class GenericRepositoryTests : RepositoryTestBase
{
    private MongoSlugRepository<Brand> _sut = null!;

    public GenericRepositoryTests(MongoServerFixture server) : base(server)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        if (SkipReason is not null)
        {
            return;
        }

        _sut = new MongoSlugRepository<Brand>(Context.Brands);
    }

    [Fact]
    public async Task AddAsync_generates_an_id_and_stamps_timestamps()
    {
        RequireMongo();

        var added = await _sut.AddAsync(TestData.Brand("stamped"));

        added.Id.Should().NotBeNullOrWhiteSpace();
        added.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        added.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        added.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_a_malformed_id_instead_of_throwing()
    {
        RequireMongo();

        // A route like /brands/not-an-objectid must produce a 404, not a 500.
        var act = async () => await _sut.GetByIdAsync("not-an-object-id");

        (await act.Should().NotThrowAsync()).Which.Should().BeNull();
    }

    [Fact]
    public async Task SoftDeleteAsync_hides_the_document_from_every_read_path()
    {
        RequireMongo();
        var brand = await _sut.AddAsync(TestData.Brand("vanishing"));

        await _sut.SoftDeleteAsync(brand.Id);

        (await _sut.GetByIdAsync(brand.Id)).Should().BeNull();
        (await _sut.GetBySlugAsync("vanishing")).Should().BeNull();
        (await _sut.ListAsync()).Should().BeEmpty();
        (await _sut.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SoftDeleteAsync_keeps_the_document_in_the_collection()
    {
        RequireMongo();
        var brand = await _sut.AddAsync(TestData.Brand("still-there"));

        await _sut.SoftDeleteAsync(brand.Id);

        var raw = await Context.Brands
            .Find(Builders<Brand>.Filter.Eq(b => b.Id, brand.Id))
            .FirstOrDefaultAsync();

        raw.Should().NotBeNull("soft delete must not remove data");
        raw.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task RestoreAsync_brings_a_soft_deleted_document_back()
    {
        RequireMongo();
        var brand = await _sut.AddAsync(TestData.Brand("returning"));
        await _sut.SoftDeleteAsync(brand.Id);

        var restored = await _sut.RestoreAsync(brand.Id);

        restored.Should().BeTrue();
        (await _sut.GetByIdAsync(brand.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_replaces_the_document_and_moves_UpdatedAt()
    {
        RequireMongo();
        var brand = await _sut.AddAsync(TestData.Brand("editable"));
        var originalUpdatedAt = brand.UpdatedAt;

        await Task.Delay(10);
        brand.Tagline = "A new tagline";
        var updated = await _sut.UpdateAsync(brand);

        updated.Should().BeTrue();
        var reloaded = await _sut.GetByIdAsync(brand.Id);
        reloaded!.Tagline.Should().Be("A new tagline");
        reloaded.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_returns_false_for_a_soft_deleted_document()
    {
        RequireMongo();
        var brand = await _sut.AddAsync(TestData.Brand("deleted-then-edited"));
        await _sut.SoftDeleteAsync(brand.Id);

        (await _sut.UpdateAsync(brand)).Should().BeFalse();
    }

    [Fact]
    public async Task PageAsync_reports_totals_correctly()
    {
        RequireMongo();
        for (var i = 1; i <= 7; i++)
        {
            await _sut.AddAsync(TestData.Brand($"brand-{i}"));
        }

        var page = await _sut.PageAsync(page: 2, pageSize: 3);

        page.Items.Should().HaveCount(3);
        page.TotalCount.Should().Be(7);
        page.TotalPages.Should().Be(3);
        page.HasPrevious.Should().BeTrue();
        page.HasNext.Should().BeTrue();
    }

    [Fact]
    public async Task PageAsync_clamps_an_out_of_range_page_size()
    {
        RequireMongo();
        await _sut.AddAsync(TestData.Brand("only-one"));

        // A client asking for 100,000 rows must not be able to ask the database for them.
        var page = await _sut.PageAsync(page: 0, pageSize: 100_000);

        page.Page.Should().Be(1);
        page.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task IsSlugAvailableAsync_excludes_the_entity_being_edited()
    {
        RequireMongo();
        var brand = await _sut.AddAsync(TestData.Brand("mine"));

        (await _sut.IsSlugAvailableAsync("mine")).Should().BeFalse();
        (await _sut.IsSlugAvailableAsync("mine", brand.Id)).Should().BeTrue();
        (await _sut.IsSlugAvailableAsync("free")).Should().BeTrue();
    }

    [Fact]
    public async Task AddManyAsync_inserts_all_and_stamps_each()
    {
        RequireMongo();

        var added = await _sut.AddManyAsync(
            [TestData.Brand("bulk-1"), TestData.Brand("bulk-2"), TestData.Brand("bulk-3")]);

        added.Should().HaveCount(3);
        added.Should().OnlyContain(brand => brand.Id != string.Empty);
        (await _sut.CountAsync()).Should().Be(3);
    }

    [Fact]
    public async Task AddManyAsync_handles_an_empty_sequence()
    {
        RequireMongo();

        var added = await _sut.AddManyAsync([]);

        added.Should().BeEmpty();
    }
}
