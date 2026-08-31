using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MSTailors.Application.Admin;
using MSTailors.Application.Auth;
using MSTailors.Application.Carts;
using MSTailors.Application.Catalog;
using MSTailors.Application.Content;
using MSTailors.Application.Customers;
using MSTailors.Application.Health;
using MSTailors.Application.Orders;

namespace MSTailors.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers Application services and every FluentValidation validator in
    /// this assembly. New validators are picked up by assembly scanning — there
    /// is no registration list to keep in sync.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IHealthService, HealthService>();

        // Public catalogue and content
        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IServiceCatalogService, ServiceCatalogService>();
        services.AddScoped<IGalleryService, GalleryService>();
        services.AddScoped<ITestimonialService, TestimonialService>();
        services.AddScoped<IBlogService, BlogService>();
        services.AddScoped<IInquiryService, InquiryService>();

        // Shopping
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<ICheckoutService, CheckoutService>();

        // Accounts
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICustomerService, CustomerService>();

        // Admin
        services.AddScoped<IAdminCatalogService, AdminCatalogService>();
        services.AddScoped<IAdminContentService, AdminContentService>();
        services.AddScoped<IAdminOrderService, AdminOrderService>();
        services.AddScoped<IAdminCustomerService, AdminCustomerService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<IAdminInquiryService, AdminInquiryService>();

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly(), includeInternalTypes: true);

        return services;
    }
}
