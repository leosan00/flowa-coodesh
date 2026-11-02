using OrderAccumulator.Application.Infrastructure;
using OrderAccumulator.Application.ReadModels;
using OrderAccumulator.Application.UseCases.GetExposures.Models;
using OrderAccumulator.Domain.Enums;

namespace OrderAccumulator.Application.UseCases.GetExposures;

public sealed class GetExposuresUseCase(IExposureReadModel readModel)
    : IUseCase<GetExposuresRequestModel, IReadOnlyList<ExposureView>>
{
    public async Task<IReadOnlyList<ExposureView>> ExecuteAsync(GetExposuresRequestModel input, CancellationToken cancellationToken = default)
    {
        var known = (await readModel.GetExposuresAsync(cancellationToken)).ToDictionary(e => e.Symbol, e => e.Value);

        return Enum.GetValues<Symbol>()
            .Select(symbol => new ExposureView(symbol, known.GetValueOrDefault(symbol)))
            .ToList();
    }
}
