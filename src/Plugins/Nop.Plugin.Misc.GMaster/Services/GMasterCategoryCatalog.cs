namespace Nop.Plugin.Misc.GMaster.Services;

public sealed record GMasterCategoryDefinition(
    string Key,
    string ArabicName,
    string EnglishName,
    string Description,
    string GlyphPath);

public static class GMasterCategoryCatalog
{
    public static readonly IReadOnlyList<GMasterCategoryDefinition> All =
    [
        new("interior-handles-trim", "مقابض وحليات داخلية", "Interior Handles & Trim",
            "مقابض أبواب، أغطية داخلية، قواعد فتيس، وحليات مقصورة.",
            "M18 19h28l4-14h24l5 14h18v22H18z M34 41h48 M30 29h56"),
        new("climate-vents", "تكييف وفتحات هواء", "Climate & Air Vents",
            "فتحات تكييف، أزرار تحكم مناخي، وأجزاء تهوية داخلية.",
            "M17 22h82v54H17z M28 35h60 M28 49h60 M28 63h60"),
        new("electrical-switches", "مفاتيح وكهرباء", "Electrical Switches & Controls",
            "مفاتيح زجاج، إضاءة، سخان، وفتح شنطة.",
            "M23 29h70v42H23z M37 42h17v16H37z M64 42h17v16H64z"),
        new("exterior-grilles", "شبكات وحليات خارجية", "Exterior Grilles & Trim",
            "شبكات أمامية، كِلى، وحليات خارجية.",
            "M14 28h88v44H14z M27 35v30 M41 35v30 M75 35v30 M89 35v30"),
        new("lighting-lenses", "إضاءة وعدسات", "Lighting & Lenses",
            "أغطية فوانيس، عدسات، كشافات ضباب، وملحقات إضاءة.",
            "M19 31c15-12 30-12 41 3v32c-14 6-28 5-41-4z M69 34c11-8 21-8 30 1v29c-10 5-20 4-30-2z"),
        new("body-underbody", "فيبر وحمايات الهيكل", "Body & Underbody Protection",
            "كرتيرات، مصفحات، بطانات رفارف، وأغطية أسفل السيارة.",
            "M19 56l13-25h52l13 25-8 16H27z M35 56h46 M42 42h32"),
        new("mirrors", "مرايات وملحقاتها", "Mirrors",
            "مرايات جانبية وأجزاء يمين ويسار لموديلات متعددة.",
            "M18 46c7-17 22-24 43-19v35c-18 8-34 3-43-16z M61 44h26l10 18H68z"),
        new("spoilers-styling", "سبويلرات وتعديلات شكلية", "Spoilers & Styling",
            "سبويلرات سقف وشنطة وملحقات مظهر رياضي.",
            "M16 58c22-15 49-21 82-16l-4 14c-31-3-54 2-75 15z"),
        new("steering-wheels", "طارات دركسيون", "Steering Wheels",
            "طارات دركسيون قياسية ورياضية وإضاءة LED.",
            "M58 22a29 29 0 1 0 0 58 29 29 0 0 0 0-58z M31 49h54 M58 50v29 M45 49l13 13 13-13"),
        new("cupholders-storage", "حوامل أكواب وتخزين", "Cupholders & Storage",
            "حوامل أكواب، أغطية مساند، وقطع تخزين داخلية.",
            "M20 27h38v48H20z M65 27h31v48H65z M29 38h20v25H29z M73 38h15v25H73z")
    ];

    public static GMasterCategoryDefinition Get(string key)
        => All.FirstOrDefault(definition => definition.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
           ?? throw new InvalidOperationException($"Unknown GMaster category key '{key}'.");
}
