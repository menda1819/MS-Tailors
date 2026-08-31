using MSTailors.Application.Catalog;
using MSTailors.Application.Common.Dtos;
using MSTailors.Application.Common.Exceptions;
using MSTailors.Application.Common.Repositories;
using MSTailors.Application.Customers.Dtos;
using MSTailors.Application.Orders;
using MSTailors.Application.Orders.Dtos;
using MSTailors.Domain.Entities;
using MSTailors.Domain.ValueObjects;

namespace MSTailors.Application.Customers;

public interface ICustomerService
{
    Task<CustomerProfileDto> GetProfileAsync(string customerId, CancellationToken cancellationToken = default);
    Task<CustomerProfileDto> UpdateProfileAsync(string customerId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AddressDto>> GetAddressesAsync(string customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AddressDto>> ReplaceAddressesAsync(string customerId, IReadOnlyList<AddressRequest> addresses, CancellationToken cancellationToken = default);
    Task<PagedResponse<OrderSummaryDto>> GetOrdersAsync(string customerId, PageRequest request, CancellationToken cancellationToken = default);
    Task<OrderDto> GetOrderAsync(string customerId, string orderNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WishlistEntryDto>> GetWishlistAsync(string customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WishlistEntryDto>> AddWishlistItemAsync(string customerId, AddWishlistItemRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WishlistEntryDto>> RemoveWishlistItemAsync(string customerId, string productId, CancellationToken cancellationToken = default);
}

public sealed class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customers;
    private readonly IOrderRepository _orders;
    private readonly IProductRepository _products;

    public CustomerService(
        ICustomerRepository customers, IOrderRepository orders, IProductRepository products)
    {
        _customers = customers;
        _orders = orders;
        _products = products;
    }

    public async Task<CustomerProfileDto> GetProfileAsync(
        string customerId, CancellationToken cancellationToken = default) =>
        (await LoadAsync(customerId, cancellationToken)).ToProfile();

    public async Task<CustomerProfileDto> UpdateProfileAsync(
        string customerId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await LoadAsync(customerId, cancellationToken);

        // Email and role are deliberately not updatable here. Changing an email
        // needs re-verification; changing a role is an admin action.
        customer.FullName = request.FullName.Trim();
        customer.Phone = request.Phone.Trim();

        await _customers.UpdateAsync(customer, cancellationToken);
        return customer.ToProfile();
    }

    public async Task<IReadOnlyList<AddressDto>> GetAddressesAsync(
        string customerId, CancellationToken cancellationToken = default) =>
        [.. (await LoadAsync(customerId, cancellationToken)).Addresses.Select(ToDto)];

    public async Task<IReadOnlyList<AddressDto>> ReplaceAddressesAsync(
        string customerId, IReadOnlyList<AddressRequest> addresses, CancellationToken cancellationToken = default)
    {
        var customer = await LoadAsync(customerId, cancellationToken);

        customer.Addresses =
        [
            .. addresses.Select(address => new Address
            {
                Label = address.Label.Trim(),
                Line1 = address.Line1.Trim(),
                Line2 = string.IsNullOrWhiteSpace(address.Line2) ? null : address.Line2.Trim(),
                City = address.City.Trim(),
                PostalCode = string.IsNullOrWhiteSpace(address.PostalCode) ? null : address.PostalCode.Trim(),
                IsDefault = address.IsDefault
            })
        ];

        // Exactly one default. Several would make checkout's choice arbitrary.
        var defaults = customer.Addresses.Where(address => address.IsDefault).ToList();

        if (defaults.Count > 1)
        {
            foreach (var extra in defaults.Skip(1))
            {
                extra.IsDefault = false;
            }
        }
        else if (defaults.Count == 0 && customer.Addresses.Count > 0)
        {
            customer.Addresses[0].IsDefault = true;
        }

        await _customers.UpdateAsync(customer, cancellationToken);
        return [.. customer.Addresses.Select(ToDto)];
    }

    public async Task<PagedResponse<OrderSummaryDto>> GetOrdersAsync(
        string customerId, PageRequest request, CancellationToken cancellationToken = default)
    {
        var page = await _orders.SearchAsync(new OrderQuery
        {
            CustomerId = customerId,
            Page = request.Page,
            PageSize = request.PageSize
        }, cancellationToken);

        return PagedResponse<OrderSummaryDto>.From(page, order => order.ToSummary());
    }

    public async Task<OrderDto> GetOrderAsync(
        string customerId, string orderNumber, CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetByOrderNumberAsync(orderNumber, cancellationToken)
            ?? throw new NotFoundException("Order", orderNumber);

        // Ownership check. Without it any signed-in customer could read any
        // order by guessing a sequential order number.
        if (order.CustomerId != customerId)
        {
            throw new NotFoundException("Order", orderNumber);
        }

        return order.ToDto();
    }

    public async Task<IReadOnlyList<WishlistEntryDto>> GetWishlistAsync(
        string customerId, CancellationToken cancellationToken = default) =>
        await ProjectWishlistAsync(await LoadAsync(customerId, cancellationToken), cancellationToken);

    public async Task<IReadOnlyList<WishlistEntryDto>> AddWishlistItemAsync(
        string customerId, AddWishlistItemRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await LoadAsync(customerId, cancellationToken);

        var product = await _products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product", request.ProductId);

        if (customer.Wishlist.Any(item => item.ProductId == product.Id))
        {
            // Idempotent: adding twice is not an error.
            return await ProjectWishlistAsync(customer, cancellationToken);
        }

        customer.Wishlist.Add(new WishlistItem
        {
            ProductId = product.Id,
            VariantSku = request.VariantSku,
            AddedAt = DateTime.UtcNow
        });

        await _customers.UpdateAsync(customer, cancellationToken);
        return await ProjectWishlistAsync(customer, cancellationToken);
    }

    public async Task<IReadOnlyList<WishlistEntryDto>> RemoveWishlistItemAsync(
        string customerId, string productId, CancellationToken cancellationToken = default)
    {
        var customer = await LoadAsync(customerId, cancellationToken);

        customer.Wishlist.RemoveAll(item => item.ProductId == productId);
        await _customers.UpdateAsync(customer, cancellationToken);

        return await ProjectWishlistAsync(customer, cancellationToken);
    }

    private async Task<IReadOnlyList<WishlistEntryDto>> ProjectWishlistAsync(
        Customer customer, CancellationToken cancellationToken)
    {
        if (customer.Wishlist.Count == 0)
        {
            return [];
        }

        var products = await _products.GetByIdsAsync(
            customer.Wishlist.Select(item => item.ProductId), cancellationToken);
        var byId = products.ToDictionary(product => product.Id);

        var entries = new List<WishlistEntryDto>();

        foreach (var item in customer.Wishlist.OrderByDescending(entry => entry.AddedAt))
        {
            // A withdrawn product silently drops out rather than 404-ing the list.
            if (!byId.TryGetValue(item.ProductId, out var product) || !product.IsActive)
            {
                continue;
            }

            entries.Add(new WishlistEntryDto(
                product.Id, product.Name, product.Slug, item.VariantSku,
                product.EffectivePrice, product.Currency,
                product.Images.OrderBy(image => image.Order).FirstOrDefault()?.Url,
                product.Variants.Any(variant => variant.InStock),
                item.AddedAt));
        }

        return entries;
    }

    private async Task<Customer> LoadAsync(string customerId, CancellationToken cancellationToken) =>
        await _customers.GetByIdAsync(customerId, cancellationToken)
        ?? throw new NotFoundException("Customer", customerId);

    private static AddressDto ToDto(Address address) => CustomerMappings.ToDto(address);
}

public static class CustomerMappings
{
    public static AddressDto ToDto(Address address) => new(
        address.Label, address.Line1, address.Line2, address.City, address.PostalCode, address.IsDefault);

    public static CustomerProfileDto ToProfile(this Customer customer) => new(
        customer.Id, customer.FullName, customer.Email, customer.Phone,
        customer.Role.ToString(), customer.EmailVerified,
        [.. customer.Addresses.Select(ToDto)], customer.CreatedAt);

    /// <summary>Admin view. Password hash and refresh tokens are never projected.</summary>
    public static AdminCustomerDto ToAdminDto(this Customer customer) => new(
        customer.Id, customer.FullName, customer.Email, customer.Phone,
        customer.Role.ToString(), customer.EmailVerified, customer.IsActive,
        customer.LastLoginAt, customer.CreatedAt);
}
