using MSTailors.Domain.Entities;

namespace MSTailors.Infrastructure.Seeding;

/// <summary>
/// The tailoring services from the brief, with real copy.
/// </summary>
/// <remarks>
/// No prices appear here by design — these pages exist to produce inquiries,
/// WhatsApp messages and measurement appointments, not transactions.
/// </remarks>
internal static class SeedServices
{
    public static List<Service> All() =>
    [
        new()
        {
            Name = "Custom Suits",
            Slug = "custom-suits",
            Category = "Suits",
            ShortDescription = "A suit cut to your measurements, your posture and the way you actually stand.",
            LongDescription =
                "A ready-made suit is cut for an average that almost nobody is. We start with a full set of " +
                "measurements — not just chest and waist, but shoulder slope, blade width and the small " +
                "asymmetries every body has — and draft a pattern from them. You choose the cloth, the lapel, " +
                "the button stance, the vents and the lining. The first fitting happens on a basted garment " +
                "that can still be taken apart, so the shape is corrected before anything is final. Most suits " +
                "take three fittings across two to three weeks.",
            BulletPoints =
            [
                "Individual pattern drafted from your measurements, kept on file for future orders",
                "Choice of British, Italian and locally woven cloth in wool, wool-linen and tropical weights",
                "Half or full canvas construction — the canvas is what lets a jacket mould to you over time",
                "Three fittings as standard, with more if the garment needs them",
                "Working cuff buttonholes, hand-finished collar and your name in the lining on request"
            ],
            ImageUrl = "/images/seed/services/custom-suits.jpg",
            DisplayOrder = 1
        },
        new()
        {
            Name = "Wedding Suits",
            Slug = "wedding-suits",
            Category = "Suits",
            ShortDescription = "The one suit in your wardrobe that will be photographed from every angle.",
            LongDescription =
                "Wedding tailoring carries a deadline that cannot move, so we work backwards from your date and " +
                "build in time for the fittings and the inevitable last-minute changes. Come to us early — eight " +
                "to ten weeks is comfortable, six is workable, and anything shorter needs a conversation first. " +
                "We dress grooms, groomsmen and fathers together so the party reads as a set rather than a " +
                "coincidence, and we keep a record of every measurement in case something needs adjusting the " +
                "week of the ceremony.",
            BulletPoints =
            [
                "Booking timeline planned backwards from your wedding date",
                "Coordinated fittings for the groom, groomsmen and family",
                "Morning coats, three-piece suits, tuxedos and national dress",
                "Silk, brocade and embroidered detailing available",
                "A final press and check in the week before the ceremony"
            ],
            ImageUrl = "/images/seed/services/wedding-suits.jpg",
            DisplayOrder = 2
        },
        new()
        {
            Name = "Business Suits",
            Slug = "business-suits",
            Category = "Suits",
            ShortDescription = "Built for five days a week, not one evening a year.",
            LongDescription =
                "A suit worn daily fails in predictable places: the seat, the elbows, the trouser hem and the " +
                "collar roll. We build business suits with that in mind — reinforced seats, generous inlays for " +
                "future alterations, and cloth chosen for recovery rather than handle alone. For clients " +
                "ordering more than one, we recommend a two-trouser suit; the jacket will comfortably outlast " +
                "two pairs of trousers.",
            BulletPoints =
            [
                "Cloth selected for crease recovery and year-round wear in a warm climate",
                "Two-trouser option, since trousers wear out first",
                "Generous seam inlays so the suit can be altered as your size changes",
                "Half-lined and unlined options for hot weather",
                "Repeat orders cut from your pattern on file, no re-measuring needed"
            ],
            ImageUrl = "/images/seed/services/business-suits.jpg",
            DisplayOrder = 3
        },
        new()
        {
            Name = "Custom Shirts",
            Slug = "custom-shirts",
            Category = "Shirts",
            ShortDescription = "Correct collar, correct sleeve, no billowing at the waist.",
            LongDescription =
                "Most men wear a shirt one size too large because the collar dictated the purchase. Cutting to " +
                "measure separates those decisions: the collar fits the neck, the body fits the torso, and the " +
                "sleeve ends where it should. We hold your pattern on file, so reordering is a phone call. " +
                "Minimum order is one shirt, though the per-shirt price improves from three.",
            BulletPoints =
            [
                "Collar, cuff and placket styles chosen individually",
                "Two-ply cotton poplin, oxford, twill and linen",
                "Single or double cuffs, with or without a chest pocket",
                "Monogramming available on the cuff or placket",
                "Pattern retained for effortless reordering"
            ],
            ImageUrl = "/images/seed/services/custom-shirts.jpg",
            DisplayOrder = 4
        },
        new()
        {
            Name = "Custom Trousers",
            Slug = "custom-trousers",
            Category = "Trousers",
            ShortDescription = "The garment where fit is most obvious and most often wrong.",
            LongDescription =
                "Trousers are unforgiving. The rise, the seat and the break decide whether they look considered " +
                "or borrowed, and none of those can be fixed by a hem alone. We cut trousers to your rise and " +
                "your preferred break, with a choice of flat front or pleats, side adjusters or belt loops. " +
                "Ordering two or three pairs at once in different cloths is the most economical way to rebuild " +
                "a working wardrobe.",
            BulletPoints =
            [
                "Rise, seat and thigh cut to your measurements",
                "Flat front, single or double pleat",
                "Side adjusters, belt loops or braces buttons",
                "Cuffed or plain hem, with your preferred break",
                "Curtained waistband and a lined seat for comfort and durability"
            ],
            ImageUrl = "/images/seed/services/custom-trousers.jpg",
            DisplayOrder = 5
        },
        new()
        {
            Name = "Blazers & Sports Jackets",
            Slug = "blazers-and-sports-jackets",
            Category = "Suits",
            ShortDescription = "The most useful jacket you can own, and the hardest to buy off the rack.",
            LongDescription =
                "A blazer does more work than a suit jacket because it goes with everything else you own. That " +
                "makes the cut matter more, not less. We build blazers slightly softer than a business jacket — " +
                "less padding, a more natural shoulder — so they sit comfortably with trousers, chinos or " +
                "denim. Patch pockets, contrast buttons and an unstructured body are all options.",
            BulletPoints =
            [
                "Soft, natural shoulder with light or no padding",
                "Unstructured and half-lined builds for warm weather",
                "Patch, flap or jetted pockets",
                "Hopsack, linen, wool-silk and cotton twill",
                "Horn, brass or mother-of-pearl buttons"
            ],
            ImageUrl = "/images/seed/services/blazers.jpg",
            DisplayOrder = 6
        },
        new()
        {
            Name = "School, Office & Corporate Uniforms",
            Slug = "uniforms",
            Category = "Uniforms",
            ShortDescription = "Consistent sizing across a whole organisation, repeatable year after year.",
            LongDescription =
                "Uniform work is a different discipline from individual tailoring: the challenge is consistency " +
                "at volume, not one perfect garment. We run a sizing session on site, build a size set your " +
                "staff or students are measured against, and keep the specification on file so next year's " +
                "order matches this year's exactly. Cloth is chosen for laundering — colourfastness and shape " +
                "retention through repeated industrial washing matter more than handle.",
            BulletPoints =
            [
                "On-site measuring sessions for schools and offices",
                "Size sets and a written specification retained between orders",
                "Cloth tested for colourfastness and shrinkage under repeated washing",
                "Embroidered logos, crests and name badges",
                "Staged delivery for large orders, with replacements available mid-year"
            ],
            ImageUrl = "/images/seed/services/uniforms.jpg",
            DisplayOrder = 7
        },
        new()
        {
            Name = "Kids' Formal Wear",
            Slug = "kids-formal-wear",
            Category = "Kidswear",
            ShortDescription = "Properly cut formal clothes that a child can actually move in.",
            LongDescription =
                "Children's formal wear is usually either miniature adult tailoring that restricts movement, or " +
                "costume that photographs badly. We cut somewhere sensible in between: real construction, real " +
                "cloth, but with room to breathe and seams that can be let out as they grow. Popular for " +
                "weddings, first communions, prize-givings and family portraits.",
            BulletPoints =
            [
                "Sized for movement, with generous inlays for growth",
                "Lightweight cloth suited to a warm climate and a long day",
                "Waistcoats, shorts suits, blazers and full three-piece suits",
                "Matched to an adult's suit for family occasions",
                "Quick turnaround for growth spurts before an event"
            ],
            ImageUrl = "/images/seed/services/kids-formal-wear.jpg",
            DisplayOrder = 8
        },
        new()
        {
            Name = "Suit & Coat Rental",
            Slug = "suit-and-coat-rental",
            Category = "Rental",
            ShortDescription = "For the occasion you will not repeat.",
            LongDescription =
                "Not every event justifies buying. Our rental range covers formal suits, morning coats and " +
                "dinner jackets in the sizes most often asked for, and every rental includes the alterations " +
                "needed to make it fit properly — a rented garment that hangs badly defeats the purpose. " +
                "Reserve early for December and the wedding season, when the popular sizes go first.",
            BulletPoints =
            [
                "Formal suits, morning coats and dinner jackets",
                "Fitting alterations included in the rental",
                "Shirt, tie, cummerbund and pocket square available",
                "Cleaning handled on return — no need to launder before bringing it back",
                "Rental fee deductible if you decide to have one made"
            ],
            IsActive = true,
            ImageUrl = "/images/seed/services/suit-and-coat-rental.jpg",
            DisplayOrder = 9
        },
        new()
        {
            Name = "Alterations",
            Slug = "alterations",
            Category = "Alterations",
            ShortDescription = "Make what you already own fit properly.",
            LongDescription =
                "The fastest improvement to most wardrobes is not a new purchase — it is altering what is " +
                "already hanging there. We take in and let out, shorten sleeves from the shoulder where the " +
                "cuff detail requires it, reshape jackets, adjust waistbands and replace linings and zips. We " +
                "will also tell you honestly when a garment is not worth altering, which saves everyone time.",
            BulletPoints =
            [
                "Trouser hemming, tapering and waist adjustment",
                "Jacket sleeve shortening from the cuff or the shoulder",
                "Taking in and letting out at the seat, waist and side seams",
                "Lining, zip and button replacement",
                "Wedding and evening wear alterations handled with priority"
            ],
            ImageUrl = "/images/seed/services/alterations.jpg",
            DisplayOrder = 10
        }
    ];
}
