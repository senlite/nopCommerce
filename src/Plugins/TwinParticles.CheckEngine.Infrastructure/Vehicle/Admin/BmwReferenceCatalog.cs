using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Infrastructure.Vehicle.Admin;

/// <summary>
/// Curated BMW launch dataset (H1.4, FR-119). This is reference <em>data</em>, not schema: the
/// vehicle tables carry no BMW-specific columns (FR-130), so a second manufacturer is added by
/// adding another catalog like this one, with no code change.
///
/// The slice covers BMW passenger generations with active spare-parts demand in the launch region.
/// It is deliberately a curated subset rather than an exhaustive catalogue; generations are expanded
/// from the Domain Owner's priority list over time. Every configuration produced here is active and
/// carries a production window, and each model and generation gets English and Arabic aliases
/// (FR-102, FR-107).
/// </summary>
public static class BmwReferenceCatalog
{
    public const string MakeCode = "BMW";
    public const string MakeName = "BMW";
    public const string MakeArabicAlias = "بي إم دبليو";

    public sealed record EngineSpec(string Code, string Name, string FuelType, int DisplacementCc, int PowerHp);

    public sealed record BodySpec(string Code, string Name, int Doors);

    public sealed record TrimSpec(string TrimName, string BodyCode, string EngineCode);

    public sealed record GenerationSpec(
        string Code,
        string Name,
        int StartYear,
        int? EndYear,
        IReadOnlyList<BodySpec> Bodies,
        IReadOnlyList<EngineSpec> Engines,
        IReadOnlyList<TrimSpec> Trims);

    public sealed record ModelSpec(string Code, string Name, string ArabicAlias, IReadOnlyList<GenerationSpec> Generations);

    public sealed record MarketSpec(string Code, string Name, string ArabicAlias);

    public static readonly IReadOnlyList<MarketSpec> Markets =
    [
        new("ECE", "Europe", "أوروبا"),
        new("GCC", "Gulf", "الخليج")
    ];

    private static readonly BodySpec Sedan = new("SEDAN", "Sedan", 4);
    private static readonly BodySpec Touring = new("TOURING", "Touring", 5);
    private static readonly BodySpec Hatch = new("HATCH", "Hatchback", 5);
    private static readonly BodySpec Sav = new("SAV", "Sports Activity Vehicle", 5);

    // Common BMW modular engines, reused across generations where historically accurate enough for a
    // parts-fitment launch slice.
    private static readonly EngineSpec N20B20 = new("N20B20", "N20 2.0L Turbo Petrol", "Petrol", 1997, 184);
    private static readonly EngineSpec B48Low = new("B48-184", "B48 2.0L Turbo Petrol 184hp", "Petrol", 1998, 184);
    private static readonly EngineSpec B48High = new("B48-258", "B48 2.0L Turbo Petrol 258hp", "Petrol", 1998, 258);
    private static readonly EngineSpec B47D20 = new("B47D20", "B47 2.0L Turbo Diesel", "Diesel", 1995, 190);
    private static readonly EngineSpec N55B30 = new("N55B30", "N55 3.0L Turbo Petrol", "Petrol", 2979, 306);
    private static readonly EngineSpec B58B30 = new("B58B30", "B58 3.0L Turbo Petrol", "Petrol", 2998, 340);

    public static readonly IReadOnlyList<ModelSpec> Models =
    [
        new("1ER", "1 Series", "الفئة الأولى",
        [
            new("F20", "F20", 2011, 2019,
                [Hatch],
                [B48Low, B47D20],
                [
                    new("118i", "HATCH", "B48-184"),
                    new("120d", "HATCH", "B47D20")
                ])
        ]),
        new("3ER", "3 Series", "الفئة الثالثة",
        [
            new("E90", "E90", 2005, 2013,
                [Sedan, Touring],
                [N20B20, N55B30],
                [
                    new("320i", "SEDAN", "N20B20"),
                    new("328i", "SEDAN", "N20B20"),
                    new("335i", "SEDAN", "N55B30"),
                    new("320i Touring", "TOURING", "N20B20")
                ]),
            new("F30", "F30", 2012, 2019,
                [Sedan, Touring],
                [B48Low, B48High, B47D20],
                [
                    new("320i", "SEDAN", "B48-184"),
                    new("330i", "SEDAN", "B48-258"),
                    new("320d", "SEDAN", "B47D20"),
                    new("330i Touring", "TOURING", "B48-258")
                ]),
            new("G20", "G20", 2019, null,
                [Sedan],
                [B48Low, B48High, B58B30],
                [
                    new("320i", "SEDAN", "B48-184"),
                    new("330i", "SEDAN", "B48-258"),
                    new("M340i", "SEDAN", "B58B30")
                ])
        ]),
        new("5ER", "5 Series", "الفئة الخامسة",
        [
            new("F10", "F10", 2010, 2017,
                [Sedan],
                [N20B20, N55B30],
                [
                    new("520i", "SEDAN", "N20B20"),
                    new("535i", "SEDAN", "N55B30")
                ]),
            new("G30", "G30", 2017, null,
                [Sedan],
                [B48High, B58B30],
                [
                    new("530i", "SEDAN", "B48-258"),
                    new("540i", "SEDAN", "B58B30")
                ])
        ]),
        new("X3", "X3", "إكس 3",
        [
            new("F25", "F25", 2010, 2017,
                [Sav],
                [N20B20, B47D20],
                [
                    new("xDrive20i", "SAV", "N20B20"),
                    new("xDrive20d", "SAV", "B47D20")
                ]),
            new("G01", "G01", 2017, null,
                [Sav],
                [B48High, B58B30],
                [
                    new("xDrive30i", "SAV", "B48-258"),
                    new("M40i", "SAV", "B58B30")
                ])
        ]),
        new("X5", "X5", "إكس 5",
        [
            new("F15", "F15", 2013, 2018,
                [Sav],
                [N55B30, B47D20],
                [
                    new("xDrive35i", "SAV", "N55B30"),
                    new("xDrive30d", "SAV", "B47D20")
                ])
        ])
    ];
}
