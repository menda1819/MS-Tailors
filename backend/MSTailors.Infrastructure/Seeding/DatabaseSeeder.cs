using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MSTailors.Domain.Entities;
using MSTailors.Domain.Enums;
using MSTailors.Infrastructure.Persistence;

namespace MSTailors.Infrastructure.Seeding;

/// <summary>
/// Populates a database with starter content.
/// </summary>
/// <remarks>
/// Invoked only by the <c>--seed</c> CLI flag, never on normal startup, so an
/// accidental restart in production cannot rewrite live data.
///
/// Idempotent by default: each collection is skipped if it already holds data.
/// Pass <c>--force</c> to clear the seeded collections first.
/// </remarks>
public sealed class DatabaseSeeder
{
    private readonly IMongoContext _context;
    private readonly SeedOptions _options;
    private readonly ILogger<DatabaseSeeder> _logger;
    private readonly IPasswordHasher<Customer> _passwordHasher;

    public DatabaseSeeder(
        IMongoContext context,
        IOptions<SeedOptions> options,
        ILogger<DatabaseSeeder> logger,
        IPasswordHasher<Customer> passwordHasher)
    {
        _context = context;
        _options = options.Value;
        _logger = logger;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Seeding database {Database} (force: {Force})",
            _context.Database.DatabaseNamespace.DatabaseName, force);

        if (force)
        {
            await ClearAsync(cancellationToken);
        }

        await SeedServicesAsync(cancellationToken);
        var categoryIds = await SeedCategoriesAsync(cancellationToken);
        var brandIds = await SeedBrandsAsync(cancellationToken);
        await SeedProductsAsync(brandIds, categoryIds, cancellationToken);
        await SeedAdminAsync(cancellationToken);

        _logger.LogInformation("Seeding complete");
    }

    private async Task ClearAsync(CancellationToken cancellationToken)
    {
        _logger.LogWarning("--force supplied: clearing seeded collections");

        var empty = Builders<Product>.Filter.Empty;
        await _context.Products.DeleteManyAsync(empty, cancellationToken);
        await _context.Brands.DeleteManyAsync(Builders<Brand>.Filter.Empty, cancellationToken);
        await _context.Categories.DeleteManyAsync(Builders<Category>.Filter.Empty, cancellationToken);
        await _context.Services.DeleteManyAsync(Builders<Service>.Filter.Empty, cancellationToken);

        // Only the seeded admin is removed. Real customer accounts are never
        // touched by the seeder, even with --force.
        await _context.Customers.DeleteOneAsync(
            Builders<Customer>.Filter.Eq(customer => customer.Email, NormalisedAdminEmail),
            cancellationToken);
    }

    private string NormalisedAdminEmail => _options.AdminEmail.Trim().ToLowerInvariant();

    private async Task SeedServicesAsync(CancellationToken cancellationToken)
    {
        if (await _context.Services.CountDocumentsAsync(
                Builders<Service>.Filter.Empty, cancellationToken: cancellationToken) > 0)
        {
            _logger.LogInformation("Services already present, skipping");
            return;
        }

        var services = SeedServices.All();
        Stamp(services);

        await _context.Services.InsertManyAsync(services, cancellationToken: cancellationToken);
        _logger.LogInformation("Inserted {Count} tailoring services", services.Count);
    }

    private async Task<Dictionary<string, string>> SeedCategoriesAsync(CancellationToken cancellationToken)
    {
        var existing = await _context.Categories
            .Find(Builders<Category>.Filter.Empty)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            _logger.LogInformation("Categories already present, skipping");
            return existing.ToDictionary(category => category.Slug, category => category.Id);
        }

        var parents = SeedCategories.Parents();
        Stamp(parents);
        await _context.Categories.InsertManyAsync(parents, cancellationToken: cancellationToken);

        var idsBySlug = parents.ToDictionary(category => category.Slug, category => category.Id);

        var children = SeedCategories.Children()
            .Select(pair =>
            {
                pair.Category.ParentCategoryId = idsBySlug[pair.ParentSlug];
                return pair.Category;
            })
            .ToList();

        Stamp(children);
        await _context.Categories.InsertManyAsync(children, cancellationToken: cancellationToken);

        foreach (var child in children)
        {
            idsBySlug[child.Slug] = child.Id;
        }

        _logger.LogInformation("Inserted {Count} categories", parents.Count + children.Count);
        return idsBySlug;
    }

    private async Task<Dictionary<string, string>> SeedBrandsAsync(CancellationToken cancellationToken)
    {
        var existing = await _context.Brands
            .Find(Builders<Brand>.Filter.Empty)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            _logger.LogInformation("Brands already present, skipping");
            return existing.ToDictionary(brand => brand.Slug, brand => brand.Id);
        }

        var brands = SeedBrands.All();
        Stamp(brands);
        await _context.Brands.InsertManyAsync(brands, cancellationToken: cancellationToken);

        _logger.LogInformation("Inserted {Count} brands", brands.Count);
        return brands.ToDictionary(brand => brand.Slug, brand => brand.Id);
    }

    private async Task SeedProductsAsync(
        IReadOnlyDictionary<string, string> brandIds,
        IReadOnlyDictionary<string, string> categoryIds,
        CancellationToken cancellationToken)
    {
        if (await _context.Products.CountDocumentsAsync(
                Builders<Product>.Filter.Empty, cancellationToken: cancellationToken) > 0)
        {
            _logger.LogInformation("Products already present, skipping");
            return;
        }

        var products = SeedProducts.Build(brandIds, categoryIds);
        Stamp(products);

        await _context.Products.InsertManyAsync(products, cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Inserted {Products} products with {Variants} variants across {Brands} brands",
            products.Count,
            products.Sum(product => product.Variants.Count),
            brandIds.Count);
    }

    private async Task SeedAdminAsync(CancellationToken cancellationToken)
    {
        var email = NormalisedAdminEmail;

        var exists = await _context.Customers
            .Find(Builders<Customer>.Filter.Eq(customer => customer.Email, email))
            .AnyAsync(cancellationToken);

        if (exists)
        {
            _logger.LogInformation("Admin account already exists, skipping");
            return;
        }

        var admin = new Customer
        {
            Email = email,
            FullName = _options.AdminFullName,
            Phone = _options.AdminPhone,
            Role = UserRole.Admin,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // The plaintext password exists only inside this call and is never
        // logged. Only the PBKDF2 hash is persisted.
        admin.PasswordHash = _passwordHasher.HashPassword(admin, _options.AdminPassword);

        await _context.Customers.InsertOneAsync(admin, cancellationToken: cancellationToken);

        _logger.LogInformation("Created admin account {Email}", email);
    }

    private static void Stamp<T>(IEnumerable<T> entities) where T : Domain.Common.Entity
    {
        var now = DateTime.UtcNow;
        foreach (var entity in entities)
        {
            entity.CreatedAt = now;
            entity.UpdatedAt = now;
            entity.IsDeleted = false;
        }
    }
}
