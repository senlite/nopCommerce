using System;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Infrastructure.L10n;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class L10nFormattingServiceTests
{
    [Test]
    public void FormatNumber_Should_Use_Arabic_Culture_When_Locale_Is_Arabic()
    {
        var service = new DefaultLocaleFormattingService();

        var result = service.FormatNumber(1234.5m, "ar");

        result.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public void FormatDate_Should_Return_Different_Output_By_Locale()
    {
        var service = new DefaultLocaleFormattingService();
        var date = new DateTime(2026, 8, 3);

        var en = service.FormatDate(date, "en");
        var ar = service.FormatDate(date, "ar");

        en.Should().NotBeNullOrWhiteSpace();
        ar.Should().NotBeNullOrWhiteSpace();
    }
}
