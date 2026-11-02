using OrderAccumulator.Domain.Enums;

namespace OrderAccumulator.Infrastructure.Persistence.Entities;

/// <summary>Linha da tabela AssetExposures: a exposição corrente de um ativo.</summary>
public sealed class AssetExposureRow
{
    public Symbol Symbol { get; set; }
    public decimal Value { get; set; }
}
