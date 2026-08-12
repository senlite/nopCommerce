namespace Nop.Plugin.Misc.GMaster.Services;

public sealed record GMasterCategoryDefinition(
    string Key,
    string ArabicName,
    string EnglishName,
    string Description,
    string GlyphPath);

public static class GMasterCategoryCatalog
{
    /// <summary>
    /// Category keys here must match the <c>category_key</c> values produced by
    /// <c>tools/build_catalog.py</c>. Glyph paths are simple original strokes drawn on a 128 viewbox.
    /// </summary>
    public static readonly IReadOnlyList<GMasterCategoryDefinition> All =
    [
        new("cooling-system", "نظام التبريد", "Cooling System",
            "رادياتيرات، ثرموستات، طلمبات مياه، خراطيم وخزانات تبريد.",
            "M30 26h56v40a28 28 0 0 1-56 0z M30 40h56 M30 54h56 M44 78v18 M72 78v18"),
        new("fuel-air-intake", "الوقود والسحب", "Fuel & Air Intake",
            "طلمبات بنزين، مواسير سحب، حساسات هواء، وصمامات EGR.",
            "M34 84V44a10 10 0 0 1 10-10h20a10 10 0 0 1 10 10v40z M44 34V24h20v10 M40 96h32"),
        new("engine-components", "أجزاء المحرك", "Engine Components",
            "أغطية تاكيهات، جوانات، بكرات، جوانات كرنك، وأجزاء داخلية للمحرك.",
            "M28 50h18l8-12h20l8 12h18v28h-18l-8 12H54l-8-12H28z M58 50a12 12 0 1 0 12 12"),
        new("engine-transmission-mounts", "كراسي المكينة والفتيس", "Engine & Transmission Mounts",
            "كراسي مطاطية للمكينة والفتيس تمتص الاهتزازات.",
            "M38 40h52v20H38z M48 60v18a10 10 0 0 0 10 10h12a10 10 0 0 0 10-10V60 M58 24v16 M72 24v16"),
        new("suspension", "نظام التعليق", "Suspension",
            "مقصات، جلب موازن، مساعدين، قواعد ياي، ومطاط صدمات.",
            "M64 22a10 10 0 1 0 0 20 10 10 0 0 0 0-20z M64 42v20 M52 62h24l-6 34H58z M52 96h24"),
        new("steering", "نظام التوجيه", "Steering",
            "علب دركسيون وأذرع تعشيق يمين وشمال.",
            "M24 64h80 M24 64l14-10 M24 64l14 10 M104 64l-14-10 M104 64l-14 10 M64 54v20"),
        new("braking", "نظام الفرامل", "Braking",
            "خراطيم فرامل أمامية وخلفية وملحقاتها.",
            "M64 28a36 36 0 1 0 0 72 36 36 0 0 0 0-72z M64 46a18 18 0 1 0 0 36 18 18 0 0 0 0-36z"),
        new("driveline-hub", "نقل الحركة والعُجل", "Driveline & Hubs",
            "رمان بلي، أطقم أنصاف، ومحاور نقل الحركة.",
            "M64 30a34 34 0 1 0 0 68 34 34 0 0 0 0-68z M64 50a14 14 0 1 0 0 28 14 14 0 0 0 0-28z M64 20v16 M64 92v16 M20 64h16 M92 64h16"),
        new("sensors-electrical", "حساسات وكهرباء", "Sensors & Electrical",
            "حساسات أكسجين وهواء، مفاتيح زجاج، وأجزاء كهربائية.",
            "M40 24h48v28l-24 52-24-52z M40 38h48 M64 24v10"),
        new("exterior-body", "الهيكل الخارجي", "Exterior & Body",
            "سبويلرات، شبكات، أذرع كبوت وباب خلفي، ومرايات.",
            "M22 60c22-16 62-16 84 0l-6 16c-24-10-48-10-72 0z M40 76v10 M84 76v10"),
        new("interior-trim", "التجهيزات الداخلية", "Interior Trim",
            "مقابض، أغطية مساند، حوامل أكواب، وحليات مقصورة.",
            "M34 30h60v54a10 10 0 0 1-10 10H44a10 10 0 0 1-10-10z M44 44h40 M44 58h40 M44 72h24"),
        new("misc-accessories", "قطع متنوعة", "Misc Accessories",
            "قطع وملحقات متنوعة لا تندرج تحت تصنيف واحد محدد.",
            "M64 24l10 20 22 3-16 16 4 22-20-11-20 11 4-22-16-16 22-3z")
    ];

    public static GMasterCategoryDefinition Get(string key)
        => All.FirstOrDefault(definition => definition.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
           ?? throw new InvalidOperationException($"Unknown GMaster category key '{key}'.");
}
