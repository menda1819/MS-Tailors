using MSTailors.Domain.Entities;

namespace MSTailors.Infrastructure.Seeding;

/// <summary>Shop taxonomy. Two top-level categories with one level of children.</summary>
internal static class SeedCategories
{
    public const string Menswear = "menswear";
    public const string Womenswear = "womenswear";

    public static List<Category> Parents() =>
    [
        new()
        {
            Name = "Menswear", Slug = Menswear, DisplayOrder = 1,
            ImageUrl = "/images/seed/categories/menswear.jpg"
        },
        new()
        {
            Name = "Womenswear", Slug = Womenswear, DisplayOrder = 2,
            ImageUrl = "/images/seed/categories/womenswear.jpg"
        }
    ];

    /// <summary>Child categories keyed by the slug of their parent.</summary>
    public static List<(string ParentSlug, Category Category)> Children() =>
    [
        (Menswear, new Category { Name = "Shirts", Slug = "shirts", DisplayOrder = 1, ImageUrl = "/images/seed/categories/shirts.jpg" }),
        (Menswear, new Category { Name = "Polos & Tees", Slug = "polos-and-tees", DisplayOrder = 2, ImageUrl = "/images/seed/categories/polos.jpg" }),
        (Menswear, new Category { Name = "Trousers", Slug = "trousers", DisplayOrder = 3, ImageUrl = "/images/seed/categories/trousers.jpg" }),
        (Menswear, new Category { Name = "Blazers & Jackets", Slug = "blazers-and-jackets", DisplayOrder = 4, ImageUrl = "/images/seed/categories/blazers.jpg" }),
        (Womenswear, new Category { Name = "Dresses", Slug = "dresses", DisplayOrder = 1, ImageUrl = "/images/seed/categories/dresses.jpg" }),
        (Womenswear, new Category { Name = "Blouses", Slug = "blouses", DisplayOrder = 2, ImageUrl = "/images/seed/categories/blouses.jpg" }),
        (Womenswear, new Category { Name = "Skirts & Trousers", Slug = "skirts-and-trousers", DisplayOrder = 3, ImageUrl = "/images/seed/categories/skirts.jpg" })
    ];
}
