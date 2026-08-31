using MSTailors.Domain.Entities;
using MSTailors.Domain.ValueObjects;

namespace MSTailors.Infrastructure.Seeding;

/// <summary>
/// Ready-made shop stock spread across the three seed brands.
/// </summary>
/// <remarks>
/// Prices are in LKR at realistic Sri Lankan retail levels. Stock lives on the
/// variants only — the product carries no quantity of its own.
/// </remarks>
internal static class SeedProducts
{
    private sealed record VariantSpec(string Size, string Color, string ColorHex, int Stock);

    private sealed record ProductSpec(
        string BrandSlug,
        string CategorySlug,
        string Name,
        string Slug,
        string SkuPrefix,
        decimal BasePrice,
        decimal? SalePrice,
        string ShortDescription,
        string Description,
        string Fabric,
        string Care,
        string[] Tags,
        (string Label, string Value)[] Specifications,
        VariantSpec[] Variants,
        bool IsFeatured = false,
        bool IsNewArrival = false);

    private static readonly string[] MensTops = ["S", "M", "L", "XL", "XXL"];
    private static readonly string[] Waists = ["30", "32", "34", "36", "38"];
    private static readonly string[] JacketSizes = ["38R", "40R", "42R", "44R"];
    private static readonly string[] WomensSizes = ["XS", "S", "M", "L", "XL"];

    /// <summary>Builds one variant per size/colour pair, with staggered stock.</summary>
    private static VariantSpec[] Grid(string[] sizes, params (string Color, string Hex)[] colors)
    {
        var variants = new List<VariantSpec>();
        var index = 0;

        foreach (var (color, hex) in colors)
        {
            foreach (var size in sizes)
            {
                // Middle sizes carry more stock, and every few variants is left
                // at zero so out-of-stock rendering has something to show.
                var stock = index % 7 == 3 ? 0 : 4 + (index % 9);
                variants.Add(new VariantSpec(size, color, hex, stock));
                index++;
            }
        }

        return [.. variants];
    }

    public static List<Product> Build(
        IReadOnlyDictionary<string, string> brandIdsBySlug,
        IReadOnlyDictionary<string, string> categoryIdsBySlug)
    {
        return Specs()
            .Select(spec => new Product
            {
                Name = spec.Name,
                Slug = spec.Slug,
                BrandId = brandIdsBySlug[spec.BrandSlug],
                CategoryIds = [categoryIdsBySlug[spec.CategorySlug]],
                ShortDescription = spec.ShortDescription,
                Description = spec.Description,
                Fabric = spec.Fabric,
                CareInstructions = spec.Care,
                BasePrice = spec.BasePrice,
                SalePrice = spec.SalePrice,
                Currency = "LKR",
                Tags = [.. spec.Tags],
                IsFeatured = spec.IsFeatured,
                IsNewArrival = spec.IsNewArrival,
                Specifications = [.. spec.Specifications.Select(s => new Specification { Label = s.Label, Value = s.Value })],
                Images =
                [
                    new ProductImage { Url = $"/images/seed/products/{spec.Slug}-1.jpg", Alt = $"{spec.Name}, front view", Order = 0 },
                    new ProductImage { Url = $"/images/seed/products/{spec.Slug}-2.jpg", Alt = $"{spec.Name}, detail of the fabric and stitching", Order = 1 }
                ],
                Variants =
                [
                    .. spec.Variants.Select(v => new ProductVariant
                    {
                        Sku = $"{spec.SkuPrefix}-{Slugify(v.Color)}-{v.Size}".ToUpperInvariant(),
                        Size = v.Size,
                        Color = v.Color,
                        ColorHex = v.ColorHex,
                        StockQuantity = v.Stock,
                        ImageUrl = $"/images/seed/products/{spec.Slug}-{Slugify(v.Color)}.jpg"
                    })
                ],
                SeoTitle = $"{spec.Name} | MS Tailors",
                SeoDescription = spec.ShortDescription
            })
            .ToList();
    }

    private static string Slugify(string value) =>
        new(value.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());

    private static List<ProductSpec> Specs() =>
    [
        // ---------------- Atelier North — precise, urban, professional ----------------
        new(SeedBrands.AtelierNorthSlug, "shirts", "Meridian Poplin Shirt", "meridian-poplin-shirt", "AN-MPS",
            8_900m, null,
            "A crisp two-ply poplin shirt cut close through the body without pulling at the button placket.",
            "The shirt Atelier North rebuilds every year and changes least. Two-ply Egyptian cotton poplin with " +
            "a semi-cutaway collar, a clean front placket and a single back pleat for movement across the " +
            "shoulders. The body is cut close but not tight — there is enough room to sit down in without a " +
            "gap appearing between buttons.",
            "100% two-ply Egyptian cotton poplin, 120s", "Machine wash cold. Hang dry. Warm iron.",
            ["shirt", "formal", "cotton", "office"],
            [("Collar", "Semi-cutaway"), ("Cuff", "Single button"), ("Fit", "Tailored"), ("Placket", "Front placket")],
            Grid(MensTops, ("White", "#FFFFFF"), ("Sky Blue", "#B8CFE3"), ("Charcoal", "#36393F")),
            IsFeatured: true),

        new(SeedBrands.AtelierNorthSlug, "shirts", "Oxford Button-Down", "oxford-button-down", "AN-OBD",
            7_450m, 6_200m,
            "A heavier oxford cloth shirt with an unlined collar that rolls properly.",
            "Oxford cloth softens with every wash, and this shirt is cut with that in mind — slightly roomier " +
            "than the poplin so it still fits after it relaxes. The collar is unlined and cut long enough to " +
            "produce a genuine roll rather than lying flat against the chest.",
            "100% cotton oxford, 140gsm", "Machine wash cold. Tumble dry low. Iron while slightly damp.",
            ["shirt", "casual", "cotton", "oxford"],
            [("Collar", "Button-down, unlined"), ("Cuff", "Single button"), ("Fit", "Regular"), ("Pocket", "None")],
            Grid(MensTops, ("White", "#FFFFFF"), ("Pale Pink", "#E7CFCB"), ("Navy", "#1F2A44"))),

        new(SeedBrands.AtelierNorthSlug, "trousers", "Kingsway Wool Trouser", "kingsway-wool-trouser", "AN-KWT",
            14_500m, null,
            "A flat-front wool trouser with side adjusters and a curtained waistband.",
            "Cut from a mid-weight tropical wool that holds a crease through a full day in humidity. Side " +
            "adjusters replace belt loops for a cleaner line under a jacket, and the waistband is curtained " +
            "and lined to the knee so the trouser hangs rather than clings.",
            "100% tropical wool, 260gsm", "Dry clean only. Steam to refresh between cleans.",
            ["trousers", "formal", "wool", "office"],
            [("Front", "Flat front"), ("Waistband", "Curtained, side adjusters"), ("Hem", "Unfinished"), ("Lining", "Lined to knee")],
            Grid(Waists, ("Charcoal", "#36393F"), ("Navy", "#1F2A44")),
            IsFeatured: true),

        new(SeedBrands.AtelierNorthSlug, "trousers", "Weekday Cotton Chino", "weekday-cotton-chino", "AN-WCC",
            9_200m, null,
            "A structured chino that reads closer to a trouser than to casual workwear.",
            "Most chinos are cut like jeans. This one borrows its pattern from the Kingsway trouser — a higher " +
            "rise, a cleaner leg line, and a proper waistband — in a compact cotton twill that keeps its shape.",
            "98% compact cotton twill, 2% elastane", "Machine wash cold. Hang dry. Warm iron.",
            ["trousers", "chino", "cotton", "smart-casual"],
            [("Front", "Flat front"), ("Rise", "Mid-high"), ("Leg", "Tapered"), ("Pockets", "Slant front, jetted back")],
            Grid(Waists, ("Stone", "#C9BBA8"), ("Olive", "#4A5240"), ("Navy", "#1F2A44"))),

        new(SeedBrands.AtelierNorthSlug, "blazers-and-jackets", "Unstructured Hopsack Blazer", "unstructured-hopsack-blazer", "AN-UHB",
            34_500m, null,
            "A half-lined blazer with a natural shoulder, built for warm weather.",
            "The jacket to reach for when a suit is too much. Hopsack weave keeps it breathable, the shoulder " +
            "carries no padding, and the body is half-lined so it can be worn through a Colombo afternoon. " +
            "Patch pockets and horn buttons keep it firmly on the casual side of tailoring.",
            "100% wool hopsack, 240gsm", "Dry clean only. Hang on a broad hanger.",
            ["blazer", "jacket", "wool", "unstructured"],
            [("Shoulder", "Natural, unpadded"), ("Lining", "Half-lined"), ("Pockets", "Patch"), ("Vents", "Double"), ("Buttons", "Horn")],
            Grid(JacketSizes, ("Navy", "#1F2A44"), ("Ink", "#14151A")),
            IsFeatured: true),

        new(SeedBrands.AtelierNorthSlug, "polos-and-tees", "Fine Knit Polo", "fine-knit-polo", "AN-FKP",
            6_800m, null,
            "A knitted polo in mercerised cotton that sits under a blazer without bulk.",
            "Knitted rather than cut from jersey, so the collar stands on its own and the hem sits flat. " +
            "Mercerised cotton gives it a slight sheen and stops it going shapeless after a few wears.",
            "100% mercerised cotton, fine gauge knit", "Hand wash cold or gentle machine cycle. Dry flat.",
            ["polo", "knitwear", "cotton", "smart-casual"],
            [("Collar", "Knitted, self-standing"), ("Placket", "Three button"), ("Gauge", "14gg"), ("Hem", "Straight")],
            Grid(MensTops, ("Ink", "#14151A"), ("Sand", "#C9BBA8"), ("Deep Green", "#2F4235"))),

        new(SeedBrands.AtelierNorthSlug, "shirts", "Linen Camp Collar Shirt", "linen-camp-collar-shirt", "AN-LCC",
            7_900m, null,
            "An open-collar linen shirt cut to be worn untucked.",
            "Cut shorter and wider than the dress shirts, with a camp collar that lies flat and a straight hem " +
            "finished to be worn out. Washed linen so it arrives already soft rather than needing a season to " +
            "break in.",
            "100% washed European linen, 165gsm", "Machine wash cold. Line dry. Iron on linen setting if desired.",
            ["shirt", "linen", "casual", "summer"],
            [("Collar", "Camp / open"), ("Hem", "Straight, untucked"), ("Fit", "Relaxed"), ("Pocket", "Single chest")],
            Grid(MensTops, ("Off White", "#F6F3EE"), ("Sage", "#9BA893")),
            IsNewArrival: true),

        // ---------------- Ceylon Loom — handwoven, natural, artisanal ----------------
        new(SeedBrands.CeylonLoomSlug, "shirts", "Handloom Stripe Shirt", "handloom-stripe-shirt", "CL-HSS",
            6_500m, null,
            "Handwoven cotton with a warp stripe that shifts slightly from panel to panel.",
            "Woven on pit looms in the Southern Province, then cut and finished by hand. The stripe is set in " +
            "the warp rather than printed, so it will not fade off the cloth. Small irregularities in the weave " +
            "are inherent to handloom and are not defects.",
            "100% handloom cotton, approx. 130gsm", "Hand wash separately for the first three washes. Line dry in shade.",
            ["shirt", "handloom", "cotton", "artisanal"],
            [("Weave", "Handloom, pit loom"), ("Origin", "Southern Province, Sri Lanka"), ("Collar", "Spread"), ("Fit", "Relaxed")],
            Grid(MensTops, ("Indigo Stripe", "#3A4A63"), ("Rust Stripe", "#9C5B3F"), ("Natural", "#E4DCCB")),
            IsFeatured: true),

        new(SeedBrands.CeylonLoomSlug, "shirts", "Khadi Cotton Kurta Shirt", "khadi-cotton-kurta-shirt", "CL-KKS",
            5_800m, 4_900m,
            "A long-line collarless shirt in hand-spun, hand-woven khadi.",
            "Khadi is spun and woven by hand, which gives it an open structure that breathes far better than " +
            "mill cloth. Cut long with side vents and a mandarin placket, it works as easily over trousers as " +
            "it does on its own.",
            "100% hand-spun khadi cotton", "Hand wash cold. Do not bleach. Dry in shade.",
            ["shirt", "khadi", "handwoven", "kurta"],
            [("Weave", "Hand-spun, hand-woven khadi"), ("Neck", "Mandarin, collarless"), ("Length", "Long, side vents")],
            Grid(MensTops, ("Natural", "#E4DCCB"), ("Charcoal", "#36393F"))),

        new(SeedBrands.CeylonLoomSlug, "trousers", "Handloom Drawstring Trouser", "handloom-drawstring-trouser", "CL-HDT",
            7_200m, null,
            "A wide-leg trouser with a drawstring waist in soft handloom cotton.",
            "Cut wide through the leg and gathered at the waist with a flat drawstring rather than elastic. " +
            "The cloth is a loose handloom weave that moves with you and dries quickly in humidity.",
            "100% handloom cotton, approx. 150gsm", "Machine wash cold on gentle. Line dry in shade.",
            ["trousers", "handloom", "relaxed", "cotton"],
            [("Waist", "Flat drawstring"), ("Leg", "Wide, straight"), ("Pockets", "Side seam"), ("Hem", "Plain")],
            Grid(["S", "M", "L", "XL"], ("Natural", "#E4DCCB"), ("Indigo", "#3A4A63"))),

        new(SeedBrands.CeylonLoomSlug, "dresses", "Handloom Wrap Dress", "handloom-wrap-dress", "CL-HWD",
            11_500m, null,
            "A true wrap dress in handwoven cotton, with no zips or hooks.",
            "A genuine wrap — it ties, so it fits a range of sizes properly rather than approximately. The " +
            "handloom cotton has enough body to hold the drape at the waist without stiffness.",
            "100% handloom cotton, approx. 160gsm", "Hand wash cold. Line dry in shade. Cool iron.",
            ["dress", "handloom", "wrap", "womenswear"],
            [("Closure", "Wrap and tie"), ("Length", "Midi"), ("Sleeve", "Three-quarter"), ("Lining", "Unlined")],
            Grid(WomensSizes, ("Indigo", "#3A4A63"), ("Terracotta", "#A8624A"), ("Natural", "#E4DCCB")),
            IsFeatured: true),

        new(SeedBrands.CeylonLoomSlug, "blouses", "Batik Panel Blouse", "batik-panel-blouse", "CL-BPB",
            8_400m, null,
            "A relaxed blouse with a hand-drawn batik panel across the front.",
            "Each batik panel is drawn by hand with wax before dyeing, so no two blouses carry exactly the same " +
            "pattern. The body is plain handloom cotton, which keeps the panel as the single focal point.",
            "100% handloom cotton with hand-drawn batik panel", "Hand wash separately in cold water. Dry in shade.",
            ["blouse", "batik", "handmade", "womenswear"],
            [("Technique", "Hand-drawn batik (canting)"), ("Neck", "Round"), ("Sleeve", "Short"), ("Fit", "Relaxed")],
            Grid(WomensSizes, ("Indigo", "#3A4A63"), ("Ochre", "#C08A3E"))),

        new(SeedBrands.CeylonLoomSlug, "skirts-and-trousers", "Handloom A-Line Skirt", "handloom-a-line-skirt", "CL-HAS",
            6_900m, null,
            "A knee-length A-line skirt with deep functional pockets.",
            "Cut with a gentle A-line and set on a faced waistband rather than elastic. The pockets are deep " +
            "enough to actually use, which is rarer than it should be.",
            "100% handloom cotton, approx. 170gsm", "Machine wash cold on gentle. Line dry. Warm iron.",
            ["skirt", "handloom", "womenswear", "cotton"],
            [("Silhouette", "A-line"), ("Length", "Knee"), ("Waistband", "Faced, concealed zip"), ("Pockets", "Deep side seam")],
            Grid(WomensSizes, ("Charcoal", "#36393F"), ("Natural", "#E4DCCB"))),

        new(SeedBrands.CeylonLoomSlug, "polos-and-tees", "Slub Cotton Tee", "slub-cotton-tee", "CL-SCT",
            3_400m, null,
            "A relaxed tee in irregular slub cotton with a soft, lived-in surface.",
            "Slub yarn gives the cloth an uneven texture that catches light and hides creasing. Cut with a " +
            "dropped shoulder and a slightly wider body than a standard tee.",
            "100% slub cotton jersey, 180gsm", "Machine wash cold inside out. Tumble dry low.",
            ["tee", "cotton", "casual", "everyday"],
            [("Neck", "Ribbed crew"), ("Shoulder", "Dropped"), ("Fit", "Relaxed")],
            Grid(MensTops, ("Natural", "#E4DCCB"), ("Charcoal", "#36393F"), ("Clay", "#A8624A"))),

        // ---------------- Harbour & Pine — casual, weekend, hard-wearing ----------------
        new(SeedBrands.HarbourAndPineSlug, "polos-and-tees", "Heavyweight Pocket Tee", "heavyweight-pocket-tee", "HP-HPT",
            3_900m, null,
            "A 240gsm tee that keeps its shape and its neckline.",
            "Most tees fail at the collar. This one uses a heavy ribbed neck taped across the shoulders, on a " +
            "240gsm body that hangs rather than clings. It is deliberately overbuilt.",
            "100% combed cotton jersey, 240gsm", "Machine wash cold. Tumble dry low. Do not bleach.",
            ["tee", "cotton", "casual", "heavyweight"],
            [("Weight", "240gsm"), ("Neck", "Ribbed, shoulder-taped"), ("Pocket", "Single chest"), ("Fit", "Regular")],
            Grid(MensTops, ("Marine", "#26414A"), ("Sand", "#C9BBA8"), ("Washed Black", "#2B2B2D")),
            IsFeatured: true),

        new(SeedBrands.HarbourAndPineSlug, "polos-and-tees", "Coast Road Polo", "coast-road-polo", "HP-CRP",
            5_200m, 4_400m,
            "A piqué polo in sun-bleached colours, cut a little longer in the body.",
            "Classic piqué with a two-button placket and a ribbed collar that survives the wash. Cut slightly " +
            "longer than standard so it stays tucked, or looks intentional untucked.",
            "100% cotton piqué, 220gsm", "Machine wash cold. Tumble dry low. Warm iron on collar.",
            ["polo", "cotton", "casual", "summer"],
            [("Knit", "Piqué"), ("Placket", "Two button"), ("Collar", "Ribbed"), ("Vents", "Side")],
            Grid(MensTops, ("Faded Blue", "#7C99AE"), ("Sand", "#C9BBA8"), ("Deep Green", "#2F4235"))),

        new(SeedBrands.HarbourAndPineSlug, "shirts", "Washed Cotton Overshirt", "washed-cotton-overshirt", "HP-WCO",
            10_800m, null,
            "A shirt-jacket in heavy washed cotton, worn open over a tee.",
            "Somewhere between a shirt and a light jacket. Heavy washed cotton twill, a boxy cut, two chest " +
            "pockets with flaps, and a hem that sits at the hip. Softens noticeably after a few washes.",
            "100% washed cotton twill, 280gsm", "Machine wash cold. Tumble dry low. Expect slight fading.",
            ["overshirt", "jacket", "cotton", "layering"],
            [("Fit", "Boxy"), ("Pockets", "Two flap chest"), ("Hem", "Straight, hip length"), ("Buttons", "Corozo")],
            Grid(MensTops, ("Olive", "#4A5240"), ("Ecru", "#E4DCCB"), ("Marine", "#26414A")),
            IsNewArrival: true),

        new(SeedBrands.HarbourAndPineSlug, "trousers", "Beach Road Short", "beach-road-short", "HP-BRS",
            4_600m, null,
            "A mid-length cotton short with a drawcord waist and real pockets.",
            "Cut at just above the knee — long enough to be respectable, short enough to be comfortable. " +
            "Drawcord waist with an inner elastic panel at the back, and pockets deep enough to hold a phone " +
            "without it falling out when you sit.",
            "100% cotton twill, 200gsm", "Machine wash cold. Hang dry.",
            ["shorts", "cotton", "casual", "summer"],
            [("Length", "Above knee"), ("Waist", "Drawcord with rear elastic"), ("Pockets", "Two side, one rear zip")],
            Grid(["S", "M", "L", "XL"], ("Sand", "#C9BBA8"), ("Marine", "#26414A"), ("Faded Blue", "#7C99AE"))),

        new(SeedBrands.HarbourAndPineSlug, "trousers", "Utility Cargo Trouser", "utility-cargo-trouser", "HP-UCT",
            8_700m, null,
            "A tapered cargo with pockets positioned so they do not bulge.",
            "Cargo pockets usually ruin the line of a trouser. These are set flatter and further back, and the " +
            "leg tapers from the knee so the whole thing reads closer to a trouser than to workwear.",
            "97% cotton ripstop, 3% elastane", "Machine wash cold. Tumble dry low.",
            ["trousers", "cargo", "cotton", "casual"],
            [("Leg", "Tapered"), ("Pockets", "Two side, two flat cargo, two rear"), ("Fabric", "Ripstop"), ("Hem", "Plain")],
            Grid(Waists, ("Olive", "#4A5240"), ("Washed Black", "#2B2B2D"))),

        new(SeedBrands.HarbourAndPineSlug, "dresses", "Jersey Weekend Dress", "jersey-weekend-dress", "HP-JWD",
            7_500m, null,
            "An easy jersey dress that packs flat and does not crease.",
            "The dress to put in a bag without thinking. Heavy cotton-modal jersey that drapes properly, a " +
            "relaxed fit through the body, and side pockets. Comes out of a suitcase ready to wear.",
            "60% cotton, 40% modal jersey, 210gsm", "Machine wash cold. Dry flat. Do not tumble dry.",
            ["dress", "jersey", "casual", "womenswear"],
            [("Length", "Midi"), ("Sleeve", "Short"), ("Pockets", "Side seam"), ("Fit", "Relaxed")],
            Grid(WomensSizes, ("Marine", "#26414A"), ("Washed Black", "#2B2B2D"), ("Sand", "#C9BBA8"))),

        new(SeedBrands.HarbourAndPineSlug, "blouses", "Linen Blend Camp Blouse", "linen-blend-camp-blouse", "HP-LCB",
            6_100m, 5_200m,
            "A boxy open-collar blouse in a cool linen-cotton blend.",
            "Cut boxy and short enough to wear untucked over a skirt or shorts. The linen-cotton blend keeps " +
            "the coolness of linen while creasing considerably less.",
            "55% linen, 45% cotton, 155gsm", "Machine wash cold on gentle. Line dry.",
            ["blouse", "linen", "casual", "womenswear"],
            [("Collar", "Camp / open"), ("Fit", "Boxy"), ("Hem", "Straight"), ("Sleeve", "Short")],
            Grid(WomensSizes, ("Off White", "#F6F3EE"), ("Sage", "#9BA893"), ("Clay", "#A8624A")))
    ];
}
