using MealPenalty.Api.Dtos;
using MealPenalty.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace MealPenalty.Api.Controllers;

[ApiController]
[Route("api/v1/simulate")]
public class SimulationController : ControllerBase
{
    private readonly SimulationService _simulation;

    public SimulationController(SimulationService simulation) => _simulation = simulation;

    [HttpPost]
    public async Task<ActionResult> Simulate(SimulateRequest request)
    {
        try
        {
            var entries = request.Entries.Select(e => e.ToDomain()).ToList();
            var results = await _simulation.SimulateAsync(entries, request.RuleSetId);
            return Ok(results);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("save")]
    public async Task<ActionResult> SimulateAndSave(SimulateRequest request)
    {
        if (!ActorHeader.TryGet(Request, out var actor))
        {
            return BadRequest($"Header '{ActorHeader.HeaderName}' is required.");
        }

        try
        {
            var entries = request.Entries.Select(e => e.ToDomain()).ToList();
            var saved = await _simulation.SimulateAndSaveAsync(entries, request.RuleSetId, actor);
            var response = saved.Select(s => new { runId = s.RunId, result = s.Result });
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
