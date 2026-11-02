using OrderAccumulator.Domain.Enums;

namespace OrderAccumulator.Infrastructure.Persistence.Entities;

public sealed class AssetExposureRow
{
    public Symbol Symbol { get; set; }
    public decimal Value { get; set; }
}
