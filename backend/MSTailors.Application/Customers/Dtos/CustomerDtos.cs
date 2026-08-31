namespace MSTailors.Application.Customers.Dtos;

public sealed record AddressDto(
    string Label, string Line1, string? Line2, string City, string? PostalCode, bool IsDefault);

public sealed record CustomerProfileDto(
    string Id, string FullName, string Email, string Phone,
    string Role, bool EmailVerified, IReadOnlyList<AddressDto> Addresses, DateTime MemberSince);

public sealed class UpdateProfileRequest
{
    public string FullName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
}

public sealed class AddressRequest
{
    public string Label { get; init; } = string.Empty;
    public string Line1 { get; init; } = string.Empty;
    public string? Line2 { get; init; }
    public string City { get; init; } = string.Empty;
    public string? PostalCode { get; init; }
    public bool IsDefault { get; init; }
}

public sealed record WishlistEntryDto(
    string ProductId, string ProductName, string ProductSlug, string? VariantSku,
    decimal Price, string Currency, string? ImageUrl, bool InStock, DateTime AddedAt);

public sealed class AddWishlistItemRequest
{
    public string ProductId { get; init; } = string.Empty;
    public string? VariantSku { get; init; }
}

/// <summary>Admin listing shape. Never exposes password or token material.</summary>
public sealed record AdminCustomerDto(
    string Id, string FullName, string Email, string Phone, string Role,
    bool EmailVerified, bool IsActive, DateTime? LastLoginAt, DateTime CreatedAt);
