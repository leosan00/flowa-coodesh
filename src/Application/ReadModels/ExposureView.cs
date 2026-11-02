using OrderAccumulator.Domain.Enums;

namespace OrderAccumulator.Application.ReadModels;

public sealed record ExposureView(Symbol Symbol, decimal Value);
