using MealPenalty.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace MealPenalty.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
public class ReportsController : ControllerBase
{
    private readonly ICalculationRunRepository _runs;

    public ReportsController(ICalculationRunRepository runs) => _runs = runs;

    [HttpGet]
    public async Task<ActionResult> List([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? label)
    {
        var runs = await _runs.ListRunsAsync(from, to, label);
        return Ok(runs);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> Get(int id)
    {
        var run = await _runs.GetByIdAsync(id);
        return run is null ? NotFound() : Ok(run);
    }
}
