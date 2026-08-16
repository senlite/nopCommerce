using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.L10n;

namespace TwinParticles.CheckEngine.Tests.Architecture;

/// <summary>
/// FR-522 glossary hit-rate gate: compliant translations must pass; misses must score below approval threshold.
/// </summary>
[TestFixture]
public class GlossaryHitRateTests
{
    private static readonly (string English, string ArabicCompliant, string ArabicNonCompliant)[] ReferenceCorpus =
    [
        ("BMW water pump", "مضخة مياه BMW", "مضخة BMW"),
        ("front brake pad set", "طقم فحمات فرامل أمامي", "طقم فرامل أمامي"),
        ("genuine oil filter", "فلتر زيت أصلي", "فلتر أصلي"),
        ("cabin air filter", "فلتر مقصورة", "فلتر داخلي"),
        ("upper radiator hose", "خرطوم رديتر علوي", "خرطوم علوي"),
        ("OEM spark plug", "شمعة إشعال OEM", "شمعة OEM"),
        ("timing belt kit", "طقم سير التوقيت", "طقم سير"),
        ("new alternator", "دينامو جديد", "مولد جديد"),
        ("radiator assembly", "رديتر كامل", "مبرد كامل"),
        ("rear shock absorber", "مساعد خلفي", "صدمة خلفية"),
        ("lower control arm", "مقص سفلي", "ذراع سفلي"),
        ("wheel bearing hub", "رولمان بلي", "محمل عجلة"),
        ("electric fuel pump", "طرمبة بنزين كهربائية", "مضخة وقود"),
        ("engine thermostat", "ثرموستات", "منظم حرارة"),
        ("clutch disc", "قرص كلتش", "قرص"),
        ("automatic transmission fluid", "زيت ناقل حركة", "زيت ATF"),
        ("front suspension link", "وصلة تعليق أمامية", "وصلة أمامية"),
        ("exhaust manifold", "مجمع عادم", "مجمع"),
        ("catalytic converter unit", "محول حفاز", "محول"),
        ("upstream oxygen sensor", "حساس أكسجين", "حساس"),
        ("head gasket set", "طقم جوان رأس", "طقم جوان")
    ];

    [Test]
    public void ReferenceCorpus_CompliantTranslations_Should_Achieve_Full_Hit_Rate()
    {
        var service = new AutomotiveGlossaryService();
        var hits = ReferenceCorpus.Count(sample => service.ScoreTranslation(sample.English, sample.ArabicCompliant) >= 1m);
        var hitRate = (double)hits / ReferenceCorpus.Length;

        hitRate.Should().BeGreaterOrEqualTo(0.95, "FR-522 compliant corpus should meet glossary hit-rate gate");
    }

    [Test]
    public void ReferenceCorpus_NonCompliantTranslations_Should_Score_Below_Approval_Threshold()
    {
        var service = new AutomotiveGlossaryService();

        foreach (var sample in ReferenceCorpus)
        {
            service.ScoreTranslation(sample.English, sample.ArabicNonCompliant)
                .Should().BeLessThan(1m, because: sample.English);
        }
    }

    [Test]
    public void ScoreTranslation_Should_Use_Admin_Overrides_For_Validation()
    {
        var service = new AutomotiveGlossaryService(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["brake pad"] = "فحمات مخصصة"
        });

        service.ScoreTranslation("front brake pad", "فحمات مخصصة أمامية").Should().Be(1m);
        service.ScoreTranslation("front brake pad", "فحمات فرامل أمامية").Should().BeLessThan(1m);
    }
}
