using MSTailors.Domain.Entities;

namespace MSTailors.Infrastructure.Seeding;

/// <summary>
/// Three placeholder brands with deliberately distinct identities, so the
/// multi-brand UI can be built and reviewed before real brands sign up.
/// </summary>
/// <remarks>
/// These are demo records, not real companies. The application must never refer
/// to them by name — brands are runtime data, and an admin can delete all three
/// without breaking anything.
/// </remarks>
internal static class SeedBrands
{
    public const string AtelierNorthSlug = "atelier-north";
    public const string CeylonLoomSlug = "ceylon-loom";
    public const string HarbourAndPineSlug = "harbour-and-pine";

    public static List<Brand> All() =>
    [
        new()
        {
            Name = "Atelier North",
            Slug = AtelierNorthSlug,
            Tagline = "Sharp lines, quiet confidence.",
            Story =
                "Atelier North began in a single upstairs room with three machines and a stubborn belief that " +
                "modern menswear had become either too loud or too lazy. The label builds a deliberately narrow " +
                "range — shirts, trousers, and outerwear — and refines the same patterns season after season " +
                "rather than chasing new ones. Every garment is cut for a clean, close silhouette that holds its " +
                "shape through a long working day.",
            Philosophy =
                "Fewer pieces, better made. Nothing enters the range until it improves on what it replaces.",
            TargetAudience =
                "Professionals in their late twenties to forties who dress well without wanting to think about it.",
            LogoUrl = "/images/seed/brands/atelier-north-logo.svg",
            HeroImageUrl = "/images/seed/brands/atelier-north-hero.jpg",
            DisplayOrder = 1,
            SeoTitle = "Atelier North — Modern Menswear",
            SeoDescription = "Precisely cut shirts, trousers and outerwear for the working week."
        },
        new()
        {
            Name = "Ceylon Loom",
            Slug = CeylonLoomSlug,
            Tagline = "Woven at home, worn everywhere.",
            Story =
                "Ceylon Loom works directly with handloom weavers across the island, buying cloth by the piece " +
                "and paying before it ships. The result is a range where no two batches are quite identical — " +
                "the weave shifts, the dye lots move, and the label treats that as the point rather than a fault. " +
                "Garments are cut generously and finished by hand, built to soften rather than wear out.",
            Philosophy =
                "Pay the weaver first. Let the cloth lead the design, not the other way around.",
            TargetAudience =
                "Buyers who care where a garment came from and prefer natural fibres and a relaxed cut.",
            LogoUrl = "/images/seed/brands/ceylon-loom-logo.svg",
            HeroImageUrl = "/images/seed/brands/ceylon-loom-hero.jpg",
            DisplayOrder = 2,
            SeoTitle = "Ceylon Loom — Handloom Clothing",
            SeoDescription = "Handwoven cotton and linen clothing made with island weavers."
        },
        new()
        {
            Name = "Harbour & Pine",
            Slug = HarbourAndPineSlug,
            Tagline = "For the days off.",
            Story =
                "Harbour & Pine makes the clothes people actually reach for on a Saturday. The range is built " +
                "around heavyweight jersey, washed cotton and a colour palette borrowed from the coast road — " +
                "sun-bleached blues, sand, and a deep marine green. Nothing in the collection needs ironing, and " +
                "everything is designed to survive being packed badly.",
            Philosophy =
                "Comfortable should not mean shapeless. Casual clothes deserve real patterns too.",
            TargetAudience =
                "Younger buyers and families shopping for weekend and holiday wear.",
            LogoUrl = "/images/seed/brands/harbour-and-pine-logo.svg",
            HeroImageUrl = "/images/seed/brands/harbour-and-pine-hero.jpg",
            DisplayOrder = 3,
            SeoTitle = "Harbour & Pine — Weekend Wear",
            SeoDescription = "Relaxed, hard-wearing casual clothing for weekends and travel."
        }
    ];
}
