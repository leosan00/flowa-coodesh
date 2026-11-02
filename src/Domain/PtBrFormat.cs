using System.Globalization;

namespace OrderAccumulator.Domain;

/// <summary>Formatação pt-BR sem depender de ICU/cultura instalada (containers costumam rodar em modo invariante).</summary>
internal static class PtBrFormat
{
    private static readonly NumberFormatInfo Info = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = "."
    };

    public static string Number(decimal value, string format) => value.ToString(format, Info);

    public static string Money(decimal value) => $"R$ {Number(value, "N2")}";
}
