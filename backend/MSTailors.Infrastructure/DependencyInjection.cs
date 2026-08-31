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
using MSTailors.Application.Orders;
using MSTailors.Infrastructure.Auth;
using MSTailors.Infrastructure.Configuration;
using MSTailors.Infrastructure.Health;
using MSTailors.Infrastructure.Media;
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
        services.AddSingleton<ReplicaSetInitializer>();
        services.AddHostedService<MongoIndexHostedService>();

        services.AddSingleton<IPasswordHasher<Customer>, PasswordHasher<Customer>>();

        services
            .AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<MSTailors.Application.Auth.ITokenService, TokenService>();
        services.AddSingleton<MSTailors.Application.Auth.IPasswordHashingService, PasswordHashingService>();

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

        AddMediaStorage(services, configuration);

        return services;
    }

    private static void AddMediaStorage(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<MediaSettings>()
            .Bind(configuration.GetSection(MediaSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var settings = configuration.GetSection(MediaSettings.SectionName).Get<MediaSettings>()
                       ?? new MediaSettings();

        if (settings.Provider == MediaStoreProvider.ObjectStorage)
        {
            if (string.IsNullOrWhiteSpace(settings.ServiceUrl)
                || string.IsNullOrWhiteSpace(settings.Bucket)
                || string.IsNullOrWhiteSpace(settings.AccessKeyId)
                || string.IsNullOrWhiteSpace(settings.SecretAccessKey))
            {
                throw new InvalidOperationException(
                    "Media:Provider is ObjectStorage but ServiceUrl, Bucket, AccessKeyId or SecretAccessKey " +
                    "is missing. Set them in user secrets (colon form) or environment variables (double underscore).");
            }

            services.AddSingleton<Amazon.S3.IAmazonS3>(_ => new Amazon.S3.AmazonS3Client(
                new Amazon.Runtime.BasicAWSCredentials(settings.AccessKeyId, settings.SecretAccessKey),
                new Amazon.S3.AmazonS3Config
                {
                    ServiceURL = settings.ServiceUrl,
                    // R2 requires path-style addressing; virtual-host style
                    // resolves to a bucket subdomain that does not exist.
                    ForcePathStyle = true,
                    AuthenticationRegion = settings.Region,
                    RequestChecksumCalculation = Amazon.Runtime.RequestChecksumCalculation.WHEN_REQUIRED,
                    ResponseChecksumValidation = Amazon.Runtime.ResponseChecksumValidation.WHEN_REQUIRED
                }));

            services.AddSingleton<MSTailors.Application.Media.IMediaStore, ObjectStorageMediaStore>();
        }
        else
        {
            services.AddSingleton<MSTailors.Application.Media.IMediaStore, LocalDiskMediaStore>();
        }
    }

    private static void AddRepositories(IServiceCollection services)
    {
        // Collections with non-trivial queries get a dedicated repository.
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<MSTailors.Application.Carts.ICouponRepository, CouponRepository>();
        services.AddScoped<IOrderTransactionStore, OrderTransactionStore>();

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

        services.AddScoped<IRepository<Inquiry>>(provider =>
            new MongoRepository<Inquiry>(Context(provider).Inquiries));
        services.AddScoped<IRepository<GalleryItem>>(provider =>
            new MongoRepository<GalleryItem>(Context(provider).GalleryItems));
        services.AddScoped<IRepository<Testimonial>>(provider =>
            new MongoRepository<Testimonial>(Context(provider).Testimonials));

        static IMongoContext Context(IServiceProvider provider) => provider.GetRequiredService<IMongoContext>();
    }
}
