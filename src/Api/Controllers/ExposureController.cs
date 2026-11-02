using Microsoft.AspNetCore.Mvc;
using OrderAccumulator.Api.Models;
using OrderAccumulator.Application.ReadModels;
using OrderAccumulator.Application.UseCases;
using OrderAccumulator.Application.UseCases.GetExposures.Models;

namespace OrderAccumulator.Api.Controllers;

[ApiController]
[Route("exposure")]
public class ExposureController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IEnumerable<ExposureResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExposures(
        [FromServices] IUseCase<GetExposuresRequestModel, IReadOnlyList<ExposureView>> getExposures,
        CancellationToken cancellationToken)
    {
        var exposures = await getExposures.ExecuteAsync(new GetExposuresRequestModel(), cancellationToken);

        return Ok(exposures.Select(ExposureResponse.From));
    }
}
