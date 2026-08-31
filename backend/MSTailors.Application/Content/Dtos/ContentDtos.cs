using MSTailors.Domain.Enums;

namespace MSTailors.Application.Content.Dtos;

public sealed record ServiceListItemDto(
    string Id, string Name, string Slug, string Category,
    string ShortDescription, string ImageUrl, int DisplayOrder);

public sealed record ServiceDetailDto(
    string Id, string Name, string Slug, string Category,
    string ShortDescription, string LongDescription, string ImageUrl,
    IReadOnlyList<string> BulletPoints, int DisplayOrder);

public sealed record GalleryItemDto(
    string Id, string ImageUrl, string ThumbnailUrl, string Caption,
    string AltText, GalleryCategory Category, string CategoryLabel, int DisplayOrder);

public sealed record TestimonialDto(
    string Id, string CustomerName, string? CustomerTitle,
    string Quote, int Rating, string? ImageUrl, int DisplayOrder);

public sealed record BlogPostListItemDto(
    string Id, string Title, string Slug, string Excerpt,
    string CoverImageUrl, string Author, IReadOnlyList<string> Tags, DateTime? PublishedAt);

public sealed record BlogPostDetailDto(
    string Id, string Title, string Slug, string Excerpt, string Content,
    string CoverImageUrl, string Author, IReadOnlyList<string> Tags, DateTime? PublishedAt);

public sealed record InquiryCreatedDto(string Id, string Message);
