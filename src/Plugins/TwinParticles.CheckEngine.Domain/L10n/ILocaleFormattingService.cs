namespace TwinParticles.CheckEngine.Domain.L10n;

public interface ILocaleFormattingService
{
    string FormatNumber(decimal value, string locale);

    string FormatDate(System.DateTime value, string locale);

    string FormatUnit(decimal value, string unitCode, string locale);
}
