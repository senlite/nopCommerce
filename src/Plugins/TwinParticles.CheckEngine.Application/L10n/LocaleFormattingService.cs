using System;
using TwinParticles.CheckEngine.Domain.L10n;

namespace TwinParticles.CheckEngine.Application.L10n;

public sealed class LocaleFormattingService
{
    private readonly ILocaleFormattingService _formatter;

    public LocaleFormattingService(ILocaleFormattingService formatter)
    {
        _formatter = formatter;
    }

    public L10nPreviewResult Preview(decimal number, DateTime date, string unitCode, string locale)
    {
        return new L10nPreviewResult
        {
            Number = _formatter.FormatNumber(number, locale),
            Date = _formatter.FormatDate(date, locale),
            Unit = _formatter.FormatUnit(number, unitCode, locale),
            Locale = locale
        };
    }
}
