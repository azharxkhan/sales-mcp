using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Tax;

namespace SalesMcp.Infrastructure.Seeding;

/// <summary>One variant of an item. Overrides apply where the variant differs from its item.</summary>
internal sealed record VariantSpec(string Label, decimal ListPrice, VatCode? Vat = null, int? ReturnWindowDays = null);

/// <summary>An item sold in three variants. <see cref="Template"/> takes the variant label as <c>{0}</c>.</summary>
internal sealed record ItemSpec(string Template, UnitOfMeasure Unit, IReadOnlyList<VariantSpec> Variants, VatCode Vat = VatCode.Standard);

/// <summary>A product category: five items of three variants each.</summary>
internal sealed record CategorySpec(string Code, string Name, int LeadTimeDays, int ReturnWindowDays, IReadOnlyList<ItemSpec> Items);

/// <summary>A configuration rule between two SKUs.</summary>
internal sealed record RuleSpec(string FromSku, OfferingRuleKind Kind, string ToSku, string Reason);

/// <summary>
/// The fixed parts of the Acme Distributors dataset: an invented South African industrial-supplies
/// distributor. Every company and person name is made up, and every email address uses the reserved
/// <c>.example</c> domain, so no real person's data is ever seeded (POPIA, guardrail E2).
/// </summary>
/// <remarks>SKUs are <c>ACM-{category}-{item}{variant:00}</c>, so <c>ACM-PTL-302</c> is power tools, item 3, variant 2.</remarks>
internal static class AcmeCatalog
{
    public const string BusinessName = "Acme Distributors";

    public const string DrillBodySku = "ACM-PTL-101";
    public const string BatterySku = "ACM-PTL-302";
    public const string InStockSku = "ACM-PPE-101";
    public const string ShortWithReceiptSku = "ACM-FAS-501";
    public const string ShortWithoutReceiptSku = "ACM-HTL-303";
    public const string DiscontinuedSku = "ACM-ELE-503";
    public const string QuantityBreaksSku = "ACM-FAS-101";
    public const string ZeroRatedSku = "ACM-LUB-501";
    public const string ExemptSku = "ACM-PPE-503";

    /// <summary>Discontinued after the history was written, so old quotes still show them.</summary>
    public static readonly IReadOnlyList<string> DiscontinuedSkus = [DiscontinuedSku, "ACM-ABR-203"];

    /// <summary>Skus with hand-set stock; never picked as random shortages.</summary>
    public static readonly IReadOnlyList<string> FixtureSkus =
        [DrillBodySku, BatterySku, InStockSku, ShortWithReceiptSku, ShortWithoutReceiptSku, DiscontinuedSku, "ACM-ABR-203", QuantityBreaksSku, ZeroRatedSku, ExemptSku];

    public static readonly IReadOnlyList<CategorySpec> Categories =
    [
        new("FAS", "Fasteners", 5, 30,
        [
            new("Hex bolt, zinc plated, {0}, box of 100", UnitOfMeasure.Box, [new("M8 x 50", 189m), new("M10 x 60", 289m), new("M12 x 80", 469m)]),
            new("Hex nut, zinc plated, {0}, box of 200", UnitOfMeasure.Box, [new("M8", 99m), new("M10", 139m), new("M12", 199m)]),
            new("Flat washer, {0}, box of 500", UnitOfMeasure.Box, [new("M8", 85m), new("M10", 110m), new("M12", 145m)]),
            new("Self-tapping screw, {0}, box of 500", UnitOfMeasure.Box, [new("8g x 25", 129m), new("10g x 32", 159m), new("12g x 50", 229m)]),
            new("Chemical anchor stud, {0}, pack of 10", UnitOfMeasure.Pack, [new("M10", 349m), new("M12", 429m), new("M16", 689m)]),
        ]),
        new("ABR", "Abrasives", 7, 30,
        [
            new("Cutting disc, steel, {0}", UnitOfMeasure.Each, [new("115 mm", 18.50m), new("230 mm", 39.90m), new("350 mm", 89m)]),
            new("Flap disc, zirconia, {0}", UnitOfMeasure.Each, [new("115 mm P40", 42m), new("115 mm P80", 42m), new("180 mm P60", 79m)]),
            new("Sanding roll, aluminium oxide, {0}, 50 m", UnitOfMeasure.Roll, [new("P80", 389m), new("P120", 389m), new("P180", 389m)]),
            new("Grinding disc, {0}", UnitOfMeasure.Each, [new("115 mm", 29.50m), new("180 mm", 54m), new("230 mm", 64m)]),
            new("Wire cup brush, {0}", UnitOfMeasure.Each, [new("75 mm M14", 129m), new("100 mm M14", 179m), new("125 mm M14", 249m)]),
        ]),
        new("PPE", "Safety and PPE", 4, 30,
        [
            new("Nitrile gloves, size {0}, box of 100", UnitOfMeasure.Box, [new("M", 149m), new("L", 149m), new("XL", 159m)]),
            new("Safety boots, steel toe, size {0}", UnitOfMeasure.Pair, [new("7", 689m), new("9", 689m), new("11", 689m)]),
            new("Safety glasses, {0}", UnitOfMeasure.Each, [new("clear", 39m), new("smoke", 45m), new("anti-fog", 59m)]),
            new("Hard hat, {0}", UnitOfMeasure.Each, [new("white", 89m), new("yellow", 89m), new("blue", 89m)]),
            new("{0}", UnitOfMeasure.Each,
            [
                new("Hi-vis vest, size L", 69m),
                new("Hi-vis vest, size XL", 69m),

                // Accredited education is VAT-exempt in South Africa; a voucher can't be returned.
                new("First-aid course voucher, level 1", 1450m, VatCode.Exempt, ReturnWindowDays: 0),
            ]),
        ]),
        new("HTL", "Hand tools", 10, 30,
        [
            new("Combination spanner set, {0}", UnitOfMeasure.Each, [new("8-piece", 459m), new("12-piece", 749m), new("22-piece", 1490m)]),
            new("Socket set, {0} drive", UnitOfMeasure.Each, [new("1/4 in", 589m), new("3/8 in", 899m), new("1/2 in", 1290m)]),
            new("Claw hammer, {0}", UnitOfMeasure.Each, [new("450 g", 219m), new("570 g", 249m), new("680 g", 289m)]),
            new("Screwdriver set, {0}", UnitOfMeasure.Each, [new("6-piece", 199m), new("10-piece", 329m), new("insulated 7-piece", 549m)]),
            new("Adjustable wrench, {0}", UnitOfMeasure.Each, [new("200 mm", 189m), new("250 mm", 239m), new("300 mm", 319m)]),
        ]),
        new("PTL", "Power tools", 14, 30,
        [
            new("Cordless drill driver, 18 V, {0}", UnitOfMeasure.Each, [new("body only", 1890m), new("with 2 Ah kit", 2690m), new("hammer action, body only", 2290m)]),
            new("Angle grinder, {0}", UnitOfMeasure.Each, [new("115 mm, 850 W", 899m), new("230 mm, 2200 W", 1690m), new("18 V cordless, body only", 2190m)]),
            new("Battery pack, 18 V, {0}", UnitOfMeasure.Each, [new("2 Ah", 849m), new("4 Ah", 1290m), new("5 Ah", 1590m)]),
            new("Battery charger, 18 V, {0}", UnitOfMeasure.Each, [new("standard", 649m), new("fast", 989m), new("dual-port", 1490m)]),
            new("Impact driver, 18 V, {0}", UnitOfMeasure.Each, [new("body only", 1990m), new("with 4 Ah kit", 3490m), new("compact, body only", 1690m)]),
        ]),
        new("ADH", "Adhesives and sealants", 7, 0,
        [
            new("Epoxy adhesive, two-part, {0}", UnitOfMeasure.Each, [new("25 ml", 89m), new("50 ml", 139m), new("250 ml", 459m)]),
            new("Silicone sealant, {0}, 280 ml", UnitOfMeasure.Each, [new("clear", 69m), new("white", 69m), new("black", 69m)]),
            new("Polyurethane sealant, {0}, 600 ml", UnitOfMeasure.Each, [new("grey", 159m), new("white", 159m), new("black", 159m)]),
            new("Cyanoacrylate glue, {0}", UnitOfMeasure.Each, [new("20 g", 59m), new("50 g", 119m), new("500 g", 849m)]),
            new("Thread locker, {0}, 50 ml", UnitOfMeasure.Each, [new("medium strength", 229m), new("high strength", 259m), new("wicking", 289m)]),
        ]),
        new("LUB", "Lubricants", 5, 0,
        [
            new("Multipurpose grease, {0}", UnitOfMeasure.Each, [new("500 g", 89m), new("3 kg", 389m), new("18 kg", 1890m)]),
            new("Hydraulic oil, ISO 68, {0}", UnitOfMeasure.Each, [new("5 L", 389m), new("20 L", 1290m), new("210 L", 11900m)]),
            new("Penetrating spray, {0}", UnitOfMeasure.Each, [new("300 ml", 79m), new("500 ml", 109m), new("5 L", 649m)]),
            new("Chain lubricant, {0}", UnitOfMeasure.Each, [new("400 ml", 119m), new("5 L", 989m), new("20 L", 3590m)]),

            // Illuminating paraffin is zero-rated in South Africa.
            new("Illuminating paraffin, {0}", UnitOfMeasure.Each, [new("5 L", 179m), new("20 L", 619m), new("210 L", 5890m)], VatCode.ZeroRated),
        ]),
        new("ELE", "Electrical", 10, 30,
        [
            new("Extension lead, {0}", UnitOfMeasure.Each, [new("5 m", 189m), new("10 m", 289m), new("20 m", 459m)]),
            new("Cable, 2.5 sq mm twin and earth, {0}", UnitOfMeasure.Roll, [new("10 m", 329m), new("50 m", 1490m), new("100 m", 2890m)]),
            new("Cable ties, {0}, pack of 100", UnitOfMeasure.Pack, [new("150 mm", 29m), new("300 mm", 59m), new("450 mm", 99m)]),
            new("Insulation tape, {0}", UnitOfMeasure.Each, [new("black", 14.50m), new("red", 14.50m), new("mixed 10-pack", 119m)]),
            new("LED work light, {0}", UnitOfMeasure.Each, [new("20 W", 349m), new("50 W", 689m), new("rechargeable 10 W", 449m)]),
        ]),
    ];

    /// <summary>Categories whose cheaper items get quantity breaks on the default list.</summary>
    public static readonly IReadOnlySet<string> QuantityBreakCategories = new HashSet<string>(StringComparer.Ordinal) { "FAS", "ABR", "PPE", "ADH", "ELE" };

    /// <summary>Categories on the mining list, at 8% below list.</summary>
    public static readonly IReadOnlySet<string> MiningCategories = new HashSet<string>(StringComparer.Ordinal) { "FAS", "ABR", "PPE", "LUB", "PTL" };

    /// <summary>Categories on the trade list, at 5% below list.</summary>
    public static readonly IReadOnlySet<string> TradeCategories = new HashSet<string>(StringComparer.Ordinal) { "HTL", "PTL", "ELE", "ADH" };

    public static readonly IReadOnlyList<RuleSpec> Rules =
    [
        new(DrillBodySku, OfferingRuleKind.Requires, BatterySku, "Sold as a body only: it needs an 18 V battery."),
        new("ACM-PTL-103", OfferingRuleKind.Requires, BatterySku, "Sold as a body only: it needs an 18 V battery."),
        new("ACM-PTL-203", OfferingRuleKind.Requires, "ACM-PTL-303", "Sold as a body only: a grinder needs the 5 Ah battery for full power."),
        new("ACM-PTL-501", OfferingRuleKind.Requires, BatterySku, "Sold as a body only: it needs an 18 V battery."),
        new("ACM-PTL-503", OfferingRuleKind.Requires, "ACM-PTL-301", "Sold as a body only: the compact driver takes the 2 Ah battery."),
        new(BatterySku, OfferingRuleKind.Recommends, "ACM-PTL-401", "Customers without an 18 V charger will need one."),
        new("ACM-PTL-102", OfferingRuleKind.Recommends, "ACM-PPE-301", "Eye protection is required when drilling."),
        new("ACM-PTL-201", OfferingRuleKind.Recommends, "ACM-ABR-101", "The matching 115 mm cutting disc."),
        new("ACM-PTL-202", OfferingRuleKind.Recommends, "ACM-ABR-102", "The matching 230 mm cutting disc."),
        new("ACM-PTL-201", OfferingRuleKind.Recommends, "ACM-PPE-301", "Eye protection is required when grinding."),
        new(QuantityBreaksSku, OfferingRuleKind.Recommends, "ACM-FAS-201", "The matching M8 nut."),
        new("ACM-FAS-102", OfferingRuleKind.Recommends, "ACM-FAS-202", "The matching M10 nut."),
        new("ACM-FAS-103", OfferingRuleKind.Recommends, "ACM-FAS-203", "The matching M12 nut."),
        new("ACM-PTL-201", OfferingRuleKind.Excludes, "ACM-ABR-102", "A 230 mm disc doesn't fit a 115 mm grinder."),
        new("ACM-PTL-202", OfferingRuleKind.Excludes, "ACM-ABR-101", "A 115 mm disc on a 230 mm grinder runs without a fitting guard."),
    ];

    /// <summary>Invented customers, largest first: revenue falls off with rank (Pareto).</summary>
    public static readonly IReadOnlyList<string> CustomerNames =
    [
        "Kestrel Ridge Mining Supplies", "Blue Crane Engineering", "Umgeni Valley Fabricators", "Ironwood Plant Hire",
        "Highveld Conveyor Services", "Sable Point Construction", "Marula Mechanical", "Drakensview Steelworks",
        "Tugela Maintenance Group", "Karoo Line Electrical", "Silverleaf Shopfitting", "Baobab Logistics Workshops",
        "Protea Pumps and Valves", "Waterberg Agri Machinery", "Coastline Marine Repairs", "Granite Peak Quarries",
        "Ember Forge Welding", "Mopane Building Contractors", "Rietvlei Packaging", "Copperbelt Cable Works",
        "Lowveld Irrigation", "Seven Hills Joinery", "Amber Dune Panelbeaters", "Thornfield Farm Services",
        "Cape Fynbos Interiors", "Zebra Crossing Signage", "Riverbend Auto Electrical", "Otterkloof Plumbing",
        "Northgate Facilities", "Kingfisher Boatworks", "Sundown Roofing", "Jacaranda Maintenance",
        "Mistbelt Timber", "Hornbill HVAC", "Saltpan Engineering", "Wild Fig Landscaping",
        "Starling Signworks", "Duiker Precision Tools", "Aloe Ridge Gates", "Quiver Tree Welding",
    ];

    // Customer traits by rank (1 = largest).
    public const int TopRank = 1;
    public const int NearCreditLimitRank = 9;
    public const int OnHoldRank = 14;
    public const int NoBackOrdersRank = 12;
    public const int CashOnlyRank = 39;
    public const int NewRank = 40;

    /// <summary>Ranks from here down have no orders before the 12-month window: they start as new customers.</summary>
    public const int FirstNewRank = 29;

    /// <summary>Ranks with no VAT number (small businesses below the registration threshold).</summary>
    public const int FirstUnregisteredRank = 36;

    public static readonly IReadOnlySet<int> NearCreditLimitRanks = new HashSet<int> { 3, NearCreditLimitRank, 17 };
    public static readonly IReadOnlySet<int> NoBackOrderRanks = new HashSet<int> { 6, NoBackOrdersRank, 20, 27, 33, 38 };
    public static readonly IReadOnlySet<int> NoPartialShipmentRanks = new HashSet<int> { NoBackOrdersRank, 26, 35 };
    public static readonly IReadOnlySet<int> CashRanks = new HashSet<int> { 38, CashOnlyRank, 40 };
    public static readonly IReadOnlySet<int> MiningRanks = new HashSet<int> { TopRank, 4, 5, 16 };
    public static readonly IReadOnlySet<int> TradeRanks = new HashSet<int> { 2, 7, 11, 17, 22, 25, 31 };
    public static readonly IReadOnlySet<int> ContractRanks = new HashSet<int> { TopRank, 2, 3, 4, 5 };

    public static readonly IReadOnlyList<string> FirstNames =
    [
        "Thandi", "Pieter", "Ayesha", "Sipho", "Lerato", "Johan", "Priya", "Bongani",
        "Annelie", "Kagiso", "Nomvula", "Ruan", "Fatima", "Themba", "Chantelle", "Mandla",
    ];

    public static readonly IReadOnlyList<string> LastNames =
    [
        "Dlamini", "Van der Merwe", "Naidoo", "Mokoena", "Botha", "Pillay", "Khumalo", "Smit",
        "Govender", "Ndlovu", "Pretorius", "Mahlangu", "Jacobs", "Nkosi", "Moodley", "Coetzee",
    ];

    /// <summary>The account number for a rank: three letters of the name and the rank, such as <c>KES-001</c>.</summary>
    public static string AccountNumberFor(int rank)
    {
        var letters = new string([.. CustomerNames[rank - 1].Where(char.IsAsciiLetter).Take(3)]).ToUpperInvariant();
        return $"{letters}-{rank:D3}";
    }
}
