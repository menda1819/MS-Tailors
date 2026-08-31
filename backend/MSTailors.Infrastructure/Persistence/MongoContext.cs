using MongoDB.Driver;
using MSTailors.Domain.Entities;

namespace MSTailors.Infrastructure.Persistence;

/// <summary>
/// Wraps the singleton <see cref="IMongoClient"/> and resolves the configured
/// database. Collection names are lowercase plural, per CLAUDE.md.
/// </summary>
public sealed class MongoContext : IMongoContext
{
    public static class Collections
    {
        public const string Brands = "brands";
        public const string Categories = "categories";
        public const string Products = "products";
        public const string Customers = "customers";
        public const string Carts = "carts";
        public const string Orders = "orders";
        public const string Coupons = "coupons";
        public const string Services = "services";
        public const string Inquiries = "inquiries";
        public const string GalleryItems = "galleryItems";
        public const string Testimonials = "testimonials";
        public const string BlogPosts = "blogPosts";
        public const string SizeGuides = "sizeGuides";
    }

    public MongoContext(IMongoClient client, string databaseName)
    {
        Database = client.GetDatabase(databaseName);
    }

    public IMongoDatabase Database { get; }

    public IMongoCollection<Brand> Brands => Database.GetCollection<Brand>(Collections.Brands);
    public IMongoCollection<Category> Categories => Database.GetCollection<Category>(Collections.Categories);
    public IMongoCollection<Product> Products => Database.GetCollection<Product>(Collections.Products);
    public IMongoCollection<Customer> Customers => Database.GetCollection<Customer>(Collections.Customers);
    public IMongoCollection<Cart> Carts => Database.GetCollection<Cart>(Collections.Carts);
    public IMongoCollection<Order> Orders => Database.GetCollection<Order>(Collections.Orders);
    public IMongoCollection<Coupon> Coupons => Database.GetCollection<Coupon>(Collections.Coupons);
    public IMongoCollection<Service> Services => Database.GetCollection<Service>(Collections.Services);
    public IMongoCollection<Inquiry> Inquiries => Database.GetCollection<Inquiry>(Collections.Inquiries);
    public IMongoCollection<GalleryItem> GalleryItems => Database.GetCollection<GalleryItem>(Collections.GalleryItems);
    public IMongoCollection<Testimonial> Testimonials => Database.GetCollection<Testimonial>(Collections.Testimonials);
    public IMongoCollection<BlogPost> BlogPosts => Database.GetCollection<BlogPost>(Collections.BlogPosts);
    public IMongoCollection<SizeGuide> SizeGuides => Database.GetCollection<SizeGuide>(Collections.SizeGuides);

    public IMongoCollection<TDocument> GetCollection<TDocument>(string name)
        => Database.GetCollection<TDocument>(name);
}
