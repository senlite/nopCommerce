using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Infrastructure.Vehicle.Admin;

/// <summary>
/// Curated BMW launch dataset (H1.4, FR-119). This is reference <em>data</em>, not schema: the
/// vehicle tables carry no BMW-specific columns (FR-130), so a second manufacturer is added by
/// adding another catalog like this one, with no code change.
///
/// The slice covers BMW passenger generations with active spare-parts demand in the launch region.
/// It is deliberately a curated structural reference rather than an exhaustive manufacturer
/// catalogue or a fitment authority: a product fits a configuration only when a separately
/// provenanced FitmentClaim says so. Every configuration carries a production window, and customer
/// search aliases cover models, generations, and configuration leaves in English and Arabic
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
    private static readonly BodySpec Coupe = new("COUPE", "Coupe", 2);
    private static readonly BodySpec Convertible = new("CONVERTIBLE", "Convertible", 2);
    private static readonly BodySpec GranCoupe = new("GRANCOUPE", "Gran Coupe", 5);
    private static readonly BodySpec Sav = new("SAV", "Sports Activity Vehicle", 5);

    // Common BMW modular engines, reused across generations where historically accurate enough for a
    // parts-fitment launch slice.
    private static readonly EngineSpec M52B20 = new("M52B20", "M52 2.0L Petrol", "Petrol", 1991, 150);
    private static readonly EngineSpec M54B22 = new("M54B22", "M54 2.2L Petrol", "Petrol", 2171, 170);
    private static readonly EngineSpec M54B25 = new("M54B25", "M54 2.5L Petrol", "Petrol", 2494, 192);
    private static readonly EngineSpec M54B30 = new("M54B30", "M54 3.0L Petrol", "Petrol", 2979, 231);
    private static readonly EngineSpec M47D20 = new("M47D20", "M47 2.0L Turbo Diesel", "Diesel", 1995, 150);
    private static readonly EngineSpec M57D30 = new("M57D30", "M57 3.0L Turbo Diesel", "Diesel", 2993, 235);
    private static readonly EngineSpec N46B20 = new("N46B20", "N46 2.0L Petrol", "Petrol", 1995, 150);
    private static readonly EngineSpec N52B25 = new("N52B25", "N52 2.5L Petrol", "Petrol", 2497, 218);
    private static readonly EngineSpec N52B30 = new("N52B30", "N52 3.0L Petrol", "Petrol", 2996, 258);
    private static readonly EngineSpec N54B30 = new("N54B30", "N54 3.0L Twin-Turbo Petrol", "Petrol", 2979, 306);
    private static readonly EngineSpec N20B20 = new("N20B20", "N20 2.0L Turbo Petrol", "Petrol", 1997, 184);
    private static readonly EngineSpec N47D20 = new("N47D20", "N47 2.0L Turbo Diesel", "Diesel", 1995, 184);
    private static readonly EngineSpec N57D30 = new("N57D30", "N57 3.0L Turbo Diesel", "Diesel", 2993, 258);
    private static readonly EngineSpec B38B15 = new("B38B15", "B38 1.5L Turbo Petrol", "Petrol", 1499, 136);
    private static readonly EngineSpec B48Low = new("B48-184", "B48 2.0L Turbo Petrol 184hp", "Petrol", 1998, 184);
    private static readonly EngineSpec B48High = new("B48-258", "B48 2.0L Turbo Petrol 258hp", "Petrol", 1998, 258);
    private static readonly EngineSpec B48M35 = new("B48-306", "B48 2.0L Turbo Petrol 306hp", "Petrol", 1998, 306);
    private static readonly EngineSpec B47D20 = new("B47D20", "B47 2.0L Turbo Diesel", "Diesel", 1995, 190);
    private static readonly EngineSpec B57D30 = new("B57D30", "B57 3.0L Turbo Diesel", "Diesel", 2993, 286);
    private static readonly EngineSpec N55B30 = new("N55B30", "N55 3.0L Turbo Petrol", "Petrol", 2979, 306);
    private static readonly EngineSpec B58B30 = new("B58B30", "B58 3.0L Turbo Petrol", "Petrol", 2998, 340);
    private static readonly EngineSpec N62B48 = new("N62B48", "N62 4.8L Petrol", "Petrol", 4799, 367);
    private static readonly EngineSpec N63B44 = new("N63B44", "N63 4.4L Twin-Turbo Petrol", "Petrol", 4395, 450);

    public static readonly IReadOnlyList<ModelSpec> Models =
    [
        new("1ER", "1 Series", "الفئة الأولى",
        [
            new("E87", "E87", 2004, 2011,
                [Hatch],
                [N46B20, N47D20],
                [
                    new("116i", "HATCH", "N46B20"),
                    new("120i", "HATCH", "N46B20"),
                    new("120d", "HATCH", "N47D20")
                ]),
            new("F20", "F20", 2011, 2019,
                [Hatch],
                [B38B15, B48Low, B47D20],
                [
                    new("116i", "HATCH", "B38B15"),
                    new("118i", "HATCH", "B38B15"),
                    new("120i", "HATCH", "B48-184"),
                    new("120d", "HATCH", "B47D20")
                ]),
            new("F40", "F40", 2019, null,
                [Hatch],
                [B38B15, B48Low, B48M35, B47D20],
                [
                    new("118i", "HATCH", "B38B15"),
                    new("120i", "HATCH", "B48-184"),
                    new("120d", "HATCH", "B47D20"),
                    new("M135i xDrive", "HATCH", "B48-306")
                ])
        ]),
        new("2ER", "2 Series", "الفئة الثانية",
        [
            new("F22", "F22", 2014, 2021,
                [Coupe, Convertible],
                [B38B15, B48Low, B47D20, B58B30],
                [
                    new("218i Coupe", "COUPE", "B38B15"),
                    new("220i Coupe", "COUPE", "B48-184"),
                    new("220d Coupe", "COUPE", "B47D20"),
                    new("M240i Coupe", "COUPE", "B58B30"),
                    new("220i Convertible", "CONVERTIBLE", "B48-184")
                ]),
            new("G42", "G42", 2021, null,
                [Coupe],
                [B48Low, B47D20, B58B30],
                [
                    new("220i Coupe", "COUPE", "B48-184"),
                    new("220d Coupe", "COUPE", "B47D20"),
                    new("M240i xDrive", "COUPE", "B58B30")
                ])
        ]),
        new("3ER", "3 Series", "الفئة الثالثة",
        [
            new("E46", "E46", 1998, 2006,
                [Sedan, Touring, Coupe, Convertible],
                [M54B22, M54B25, M54B30, M47D20],
                [
                    new("320i Sedan", "SEDAN", "M54B22"),
                    new("325i Sedan", "SEDAN", "M54B25"),
                    new("330i Sedan", "SEDAN", "M54B30"),
                    new("320d Sedan", "SEDAN", "M47D20"),
                    new("325i Touring", "TOURING", "M54B25"),
                    new("330Ci Coupe", "COUPE", "M54B30"),
                    new("330Ci Convertible", "CONVERTIBLE", "M54B30")
                ]),
            new("E90", "E90", 2005, 2013,
                [Sedan, Touring],
                [N46B20, N52B25, N52B30, N54B30, N55B30],
                [
                    new("320i", "SEDAN", "N46B20"),
                    new("325i", "SEDAN", "N52B25"),
                    new("328i", "SEDAN", "N52B30"),
                    new("335i N54", "SEDAN", "N54B30"),
                    new("335i", "SEDAN", "N55B30"),
                    new("325i Touring", "TOURING", "N52B25")
                ]),
            new("F30", "F30", 2012, 2019,
                [Sedan, Touring],
                [N20B20, N47D20, B48Low, B48High, B47D20],
                [
                    new("320i N20", "SEDAN", "N20B20"),
                    new("328i", "SEDAN", "N20B20"),
                    new("320i", "SEDAN", "B48-184"),
                    new("330i", "SEDAN", "B48-258"),
                    new("320d N47", "SEDAN", "N47D20"),
                    new("320d", "SEDAN", "B47D20"),
                    new("330i Touring", "TOURING", "B48-258")
                ]),
            new("G20", "G20", 2019, null,
                [Sedan],
                [B48Low, B48High, B47D20, B58B30],
                [
                    new("318i", "SEDAN", "B48-184"),
                    new("320i", "SEDAN", "B48-184"),
                    new("330i", "SEDAN", "B48-258"),
                    new("320d", "SEDAN", "B47D20"),
                    new("M340i", "SEDAN", "B58B30")
                ])
        ]),
        new("4ER", "4 Series", "الفئة الرابعة",
        [
            new("F32", "F32", 2013, 2020,
                [Coupe, Convertible, GranCoupe],
                [N20B20, N55B30, B48High, B58B30, B47D20],
                [
                    new("420i Coupe", "COUPE", "N20B20"),
                    new("430i Coupe", "COUPE", "B48-258"),
                    new("435i Coupe", "COUPE", "N55B30"),
                    new("440i Coupe", "COUPE", "B58B30"),
                    new("420d Gran Coupe", "GRANCOUPE", "B47D20"),
                    new("430i Convertible", "CONVERTIBLE", "B48-258")
                ]),
            new("G22", "G22", 2020, null,
                [Coupe, Convertible, GranCoupe],
                [B48Low, B48High, B47D20, B58B30],
                [
                    new("420i Coupe", "COUPE", "B48-184"),
                    new("430i Coupe", "COUPE", "B48-258"),
                    new("M440i xDrive Coupe", "COUPE", "B58B30"),
                    new("420d Gran Coupe", "GRANCOUPE", "B47D20"),
                    new("430i Convertible", "CONVERTIBLE", "B48-258")
                ])
        ]),
        new("5ER", "5 Series", "الفئة الخامسة",
        [
            new("E39", "E39", 1995, 2004,
                [Sedan, Touring],
                [M52B20, M54B25, M54B30, M57D30],
                [
                    new("520i", "SEDAN", "M52B20"),
                    new("525i", "SEDAN", "M54B25"),
                    new("530i", "SEDAN", "M54B30"),
                    new("530d", "SEDAN", "M57D30"),
                    new("525i Touring", "TOURING", "M54B25")
                ]),
            new("E60", "E60", 2003, 2010,
                [Sedan, Touring],
                [N46B20, N52B25, N52B30, M57D30],
                [
                    new("520i", "SEDAN", "N46B20"),
                    new("525i", "SEDAN", "N52B25"),
                    new("530i", "SEDAN", "N52B30"),
                    new("530d", "SEDAN", "M57D30"),
                    new("525i Touring", "TOURING", "N52B25")
                ]),
            new("F10", "F10", 2010, 2017,
                [Sedan, Touring],
                [N20B20, N47D20, N55B30, N57D30, N63B44],
                [
                    new("520i", "SEDAN", "N20B20"),
                    new("528i", "SEDAN", "N20B20"),
                    new("520d", "SEDAN", "N47D20"),
                    new("535i", "SEDAN", "N55B30"),
                    new("550i", "SEDAN", "N63B44"),
                    new("530d", "SEDAN", "N57D30"),
                    new("520d Touring", "TOURING", "N47D20")
                ]),
            new("G30", "G30", 2017, 2023,
                [Sedan, Touring],
                [B48Low, B48High, B47D20, B58B30, B57D30],
                [
                    new("520i", "SEDAN", "B48-184"),
                    new("530i", "SEDAN", "B48-258"),
                    new("520d", "SEDAN", "B47D20"),
                    new("540i", "SEDAN", "B58B30"),
                    new("530d", "SEDAN", "B57D30"),
                    new("520d Touring", "TOURING", "B47D20")
                ]),
            new("G60", "G60", 2023, null,
                [Sedan],
                [B48Low, B48High, B47D20],
                [
                    new("520i", "SEDAN", "B48-184"),
                    new("530i", "SEDAN", "B48-258"),
                    new("520d", "SEDAN", "B47D20")
                ])
        ]),
        new("7ER", "7 Series", "الفئة السابعة",
        [
            new("E65", "E65", 2001, 2008,
                [Sedan],
                [M54B30, N52B30, N62B48],
                [
                    new("730i M54", "SEDAN", "M54B30"),
                    new("730i", "SEDAN", "N52B30"),
                    new("750i", "SEDAN", "N62B48")
                ]),
            new("F01", "F01", 2008, 2015,
                [Sedan],
                [N52B30, N54B30, N55B30, N57D30, N63B44],
                [
                    new("730i", "SEDAN", "N52B30"),
                    new("740i N54", "SEDAN", "N54B30"),
                    new("740i", "SEDAN", "N55B30"),
                    new("730d", "SEDAN", "N57D30"),
                    new("750i", "SEDAN", "N63B44")
                ]),
            new("G11", "G11", 2015, 2022,
                [Sedan],
                [B48High, B58B30, B57D30, N63B44],
                [
                    new("730i", "SEDAN", "B48-258"),
                    new("740i", "SEDAN", "B58B30"),
                    new("730d", "SEDAN", "B57D30"),
                    new("750i", "SEDAN", "N63B44")
                ]),
            new("G70", "G70", 2022, null,
                [Sedan],
                [B58B30, B57D30],
                [
                    new("740i", "SEDAN", "B58B30"),
                    new("740d xDrive", "SEDAN", "B57D30")
                ])
        ]),
        new("X1", "X1", "إكس 1",
        [
            new("E84", "E84", 2009, 2015,
                [Sav],
                [N46B20, N20B20, N47D20],
                [
                    new("sDrive18i", "SAV", "N46B20"),
                    new("xDrive28i", "SAV", "N20B20"),
                    new("xDrive20d", "SAV", "N47D20")
                ]),
            new("F48", "F48", 2015, 2022,
                [Sav],
                [B38B15, B48Low, B47D20],
                [
                    new("sDrive18i", "SAV", "B38B15"),
                    new("xDrive20i", "SAV", "B48-184"),
                    new("xDrive20d", "SAV", "B47D20")
                ]),
            new("U11", "U11", 2022, null,
                [Sav],
                [B38B15, B48High, B47D20],
                [
                    new("sDrive18i", "SAV", "B38B15"),
                    new("xDrive23i", "SAV", "B48-258"),
                    new("xDrive23d", "SAV", "B47D20")
                ])
        ]),
        new("X3", "X3", "إكس 3",
        [
            new("E83", "E83", 2003, 2010,
                [Sav],
                [M54B25, M54B30, M47D20],
                [
                    new("2.5i", "SAV", "M54B25"),
                    new("3.0i", "SAV", "M54B30"),
                    new("2.0d", "SAV", "M47D20")
                ]),
            new("F25", "F25", 2010, 2017,
                [Sav],
                [N20B20, N55B30, N47D20, B47D20],
                [
                    new("xDrive20i", "SAV", "N20B20"),
                    new("xDrive28i", "SAV", "N20B20"),
                    new("xDrive35i", "SAV", "N55B30"),
                    new("xDrive20d N47", "SAV", "N47D20"),
                    new("xDrive20d", "SAV", "B47D20")
                ]),
            new("G01", "G01", 2017, null,
                [Sav],
                [B48Low, B48High, B47D20, B57D30, B58B30],
                [
                    new("xDrive20i", "SAV", "B48-184"),
                    new("xDrive30i", "SAV", "B48-258"),
                    new("xDrive20d", "SAV", "B47D20"),
                    new("xDrive30d", "SAV", "B57D30"),
                    new("M40i", "SAV", "B58B30")
                ])
        ]),
        new("X5", "X5", "إكس 5",
        [
            new("E70", "E70", 2006, 2013,
                [Sav],
                [N52B30, N55B30, M57D30, N57D30, N63B44],
                [
                    new("xDrive30i", "SAV", "N52B30"),
                    new("xDrive35i", "SAV", "N55B30"),
                    new("xDrive30d M57", "SAV", "M57D30"),
                    new("xDrive30d", "SAV", "N57D30"),
                    new("xDrive50i", "SAV", "N63B44")
                ]),
            new("F15", "F15", 2013, 2018,
                [Sav],
                [N55B30, N57D30, B47D20, N63B44],
                [
                    new("xDrive35i", "SAV", "N55B30"),
                    new("xDrive25d", "SAV", "B47D20"),
                    new("xDrive30d", "SAV", "N57D30"),
                    new("xDrive40d", "SAV", "N57D30"),
                    new("xDrive50i", "SAV", "N63B44")
                ]),
            new("G05", "G05", 2018, null,
                [Sav],
                [B58B30, B57D30, N63B44],
                [
                    new("xDrive40i", "SAV", "B58B30"),
                    new("xDrive30d", "SAV", "B57D30"),
                    new("xDrive40d", "SAV", "B57D30"),
                    new("M50i", "SAV", "N63B44")
                ])
        ]),
        new("X6", "X6", "إكس 6",
        [
            new("E71", "E71", 2008, 2014,
                [Sav],
                [N55B30, M57D30, N57D30, N63B44],
                [
                    new("xDrive35i", "SAV", "N55B30"),
                    new("xDrive30d M57", "SAV", "M57D30"),
                    new("xDrive40d", "SAV", "N57D30"),
                    new("xDrive50i", "SAV", "N63B44")
                ]),
            new("F16", "F16", 2014, 2019,
                [Sav],
                [N55B30, N57D30, N63B44],
                [
                    new("xDrive35i", "SAV", "N55B30"),
                    new("xDrive30d", "SAV", "N57D30"),
                    new("xDrive40d", "SAV", "N57D30"),
                    new("xDrive50i", "SAV", "N63B44")
                ]),
            new("G06", "G06", 2019, null,
                [Sav],
                [B58B30, B57D30, N63B44],
                [
                    new("xDrive40i", "SAV", "B58B30"),
                    new("xDrive30d", "SAV", "B57D30"),
                    new("xDrive40d", "SAV", "B57D30"),
                    new("M50i", "SAV", "N63B44")
                ])
        ])
    ];
}
