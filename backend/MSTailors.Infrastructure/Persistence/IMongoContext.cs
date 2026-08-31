using MongoDB.Driver;
using MSTailors.Domain.Entities;

namespace MSTailors.Infrastructure.Persistence;

/// <summary>
/// Typed access to the mstailors collections.
/// </summary>
/// <remarks>
/// Lives in Infrastructure, not Application, because it exposes MongoDB.Driver
/// types. Application depends on the repository interfaces instead.
/// </remarks>
public interface IMongoContext
{
    IMongoDatabase Database { get; }

    IMongoCollection<Brand> Brands { get; }
    IMongoCollection<Category> Categories { get; }
    IMongoCollection<Product> Products { get; }
    IMongoCollection<Customer> Customers { get; }
    IMongoCollection<Cart> Carts { get; }
    IMongoCollection<Order> Orders { get; }
    IMongoCollection<Coupon> Coupons { get; }
    IMongoCollection<Service> Services { get; }
    IMongoCollection<Inquiry> Inquiries { get; }
    IMongoCollection<GalleryItem> GalleryItems { get; }
    IMongoCollection<Testimonial> Testimonials { get; }
    IMongoCollection<BlogPost> BlogPosts { get; }
    IMongoCollection<SizeGuide> SizeGuides { get; }

    IMongoCollection<TDocument> GetCollection<TDocument>(string name);
}
