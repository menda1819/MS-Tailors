using FluentAssertions;
using MongoDB.Driver;
using MSTailors.Domain.ValueObjects;
using MSTailors.Infrastructure.Repositories;
using MSTailors.Tests.Fixtures;

namespace MSTailors.Tests.Repositories;

public class CustomerRepositoryTests : RepositoryTestBase
{
    private CustomerRepository _sut = null!;

    public CustomerRepositoryTests(MongoServerFixture server) : base(server)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        if (SkipReason is not null)
        {
            return;
        }

        _sut = new CustomerRepository(Context);
    }

    [Fact]
    public async Task AddAsync_normalises_the_email_to_lowercase()
    {
        RequireMongo();

        var added = await _sut.AddAsync(TestData.Customer("Mixed.Case@Example.COM"));

        added.Email.Should().Be("mixed.case@example.com");
    }

    [Fact]
    public async Task Emails_differing_only_by_case_collide_on_the_unique_index()
    {
        RequireMongo();
        await _sut.AddAsync(TestData.Customer("dup@example.com"));

        // Normalisation on write is what makes the unique index effective;
        // without it "DUP@example.com" would be accepted as a second account.
        var act = async () => await _sut.AddAsync(TestData.Customer("DUP@example.com"));

        await act.Should().ThrowAsync<MongoWriteException>()
            .Where(ex => ex.WriteError.Category == ServerErrorCategory.DuplicateKey);
    }

    [Fact]
    public async Task GetByEmailAsync_is_case_insensitive()
    {
        RequireMongo();
        await _sut.AddAsync(TestData.Customer("someone@example.com"));

        var found = await _sut.GetByEmailAsync("SOMEONE@EXAMPLE.COM");

        found.Should().NotBeNull();
    }

    [Fact]
    public async Task IsEmailAvailableAsync_excludes_the_account_being_edited()
    {
        RequireMongo();
        var customer = await _sut.AddAsync(TestData.Customer("editing@example.com"));

        (await _sut.IsEmailAvailableAsync("editing@example.com")).Should().BeFalse();
        (await _sut.IsEmailAvailableAsync("editing@example.com", customer.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task GetByRefreshTokenAsync_finds_the_owning_customer()
    {
        RequireMongo();
        var customer = TestData.Customer("tokens@example.com");
        customer.RefreshTokens =
        [
            new RefreshTokenEntry { Token = "hash-one", ExpiresAt = DateTime.UtcNow.AddDays(7) },
            new RefreshTokenEntry { Token = "hash-two", ExpiresAt = DateTime.UtcNow.AddDays(7) }
        ];
        await _sut.AddAsync(customer);

        var found = await _sut.GetByRefreshTokenAsync("hash-two");

        found.Should().NotBeNull();
        found!.Email.Should().Be("tokens@example.com");
        (await _sut.GetByRefreshTokenAsync("unknown-hash")).Should().BeNull();
    }

    [Fact]
    public async Task Addresses_and_wishlist_round_trip()
    {
        RequireMongo();
        var customer = TestData.Customer("nested@example.com");
        customer.Addresses =
        [
            new Address { Label = "Home", Line1 = "28 Station Road", City = "Panadura", IsDefault = true }
        ];
        customer.Wishlist =
        [
            new WishlistItem { ProductId = "507f1f77bcf86cd799439011", VariantSku = "SKU-M" }
        ];

        await _sut.AddAsync(customer);

        var reloaded = await _sut.GetByEmailAsync("nested@example.com");
        reloaded!.Addresses.Should().ContainSingle().Which.City.Should().Be("Panadura");
        reloaded.Wishlist.Should().ContainSingle().Which.VariantSku.Should().Be("SKU-M");
    }
}
