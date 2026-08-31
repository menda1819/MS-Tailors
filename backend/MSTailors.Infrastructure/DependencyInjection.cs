using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MSTailors.Application.Carts;
using MSTailors.Application.Catalog;
using MSTailors.Application.Common.Interfaces;
using MSTailors.Application.Common.Repositories;
using MSTailors.Application.Orders;
using MSTailors.Domain.Entities;
using MSTailors.Infrastructure.Configuration;
using MSTailors.Infrastructure.Health;
using MSTailors.Infrastructure.Persistence;
using MSTailors.Infrastructure.Repositories;
using MSTailors.Infrastructure.Seeding;

namespace MSTailors.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        MongoConventions.Register();

        services
            .AddOptions<MongoDbSettings>()
            .Bind(configuration.GetSection(MongoDbSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // One IMongoClient for the process. The driver pools connections
        // internally, so creating more than one is a resource leak, not parallelism.
        services.AddSingleton<IMongoClient>(provider =>
        {
            var settings = provider.GetRequiredService<IOptions<MongoDbSettings>>().Value;

            var clientSettings = MongoClientSettings.FromConnectionString(settings.ConnectionString);
            clientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(settings.ServerSelectionTimeoutSeconds);
            clientSettings.ApplicationName = "MSTailors.Api";

            return new MongoClient(clientSettings);
        });

        services.AddSingleton<IMongoContext>(provider =>
        {
            var settings = provider.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            return new MongoContext(provider.GetRequiredService<IMongoClient>(), settings.DatabaseName);
        });

        services.AddScoped<IDatabaseHealthProbe, MongoHealthProbe>();

        AddRepositories(services);

        services.AddSingleton<MongoIndexInitializer>();
        services.AddHostedService<MongoIndexHostedService>();

        services.AddSingleton<IPasswordHasher<Customer>, PasswordHasher<Customer>>();

        // ValidateDataAnnotations, not just Bind: without it a missing
        // Seed__AdminPassword silently seeds an admin whose password hash is
        // derived from the empty string. ValidateOnStart is deliberately not
        // used, because the API must start fine when seeding is not being run —
        // the seeder resolves IOptions and triggers validation at that point.
        services
            .AddOptions<SeedOptions>()
            .Bind(configuration.GetSection(SeedOptions.SectionName))
            .ValidateDataAnnotations();
        services.AddScoped<DatabaseSeeder>();

        return services;
    }

    private static void AddRepositories(IServiceCollection services)
    {
        // Collections with non-trivial queries get a dedicated repository.
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();

        // Everything else is served by the generic repository. Slug-backed
        // entities get the slug variant so public pages can resolve by URL.
        services.AddScoped<ISlugRepository<Brand>>(provider =>
            new MongoSlugRepository<Brand>(Context(provider).Brands));
        services.AddScoped<ISlugRepository<Category>>(provider =>
            new MongoSlugRepository<Category>(Context(provider).Categories));
        services.AddScoped<ISlugRepository<Service>>(provider =>
            new MongoSlugRepository<Service>(Context(provider).Services));
        services.AddScoped<ISlugRepository<BlogPost>>(provider =>
            new MongoSlugRepository<BlogPost>(Context(provider).BlogPosts));
        services.AddScoped<ISlugRepository<SizeGuide>>(provider =>
            new MongoSlugRepository<SizeGuide>(Context(provider).SizeGuides));

        services.AddScoped<IRepository<Coupon>>(provider =>
            new MongoRepository<Coupon>(Context(provider).Coupons));
        services.AddScoped<IRepository<Inquiry>>(provider =>
            new MongoRepository<Inquiry>(Context(provider).Inquiries));
        services.AddScoped<IRepository<GalleryItem>>(provider =>
            new MongoRepository<GalleryItem>(Context(provider).GalleryItems));
        services.AddScoped<IRepository<Testimonial>>(provider =>
            new MongoRepository<Testimonial>(Context(provider).Testimonials));

        static IMongoContext Context(IServiceProvider provider) => provider.GetRequiredService<IMongoContext>();
    }
}
