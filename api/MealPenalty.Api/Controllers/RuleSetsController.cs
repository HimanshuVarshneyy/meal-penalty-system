using MealPenalty.Api.Dtos;
using MealPenalty.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace MealPenalty.Api.Controllers;

[ApiController]
[Route("api/v1/rulesets")]
public class RuleSetsController : ControllerBase
{
    private readonly RuleSetService _ruleSets;

    public RuleSetsController(RuleSetService ruleSets) => _ruleSets = ruleSets;

    [HttpGet]
    public async Task<ActionResult<List<RuleSetSummaryDto>>> List()
    {
        var sets = await _ruleSets.ListAsync();
        return sets.Select(RuleSetSummaryDto.FromDomain).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RuleSetDetailDto>> Get(int id)
    {
        var ruleSet = await _ruleSets.GetAsync(id);
        return ruleSet is null ? NotFound() : RuleSetDetailDto.FromDomain(ruleSet);
    }

    [HttpGet("active")]
    public async Task<ActionResult<RuleSetDetailDto>> GetActive()
    {
        var ruleSet = await _ruleSets.GetActiveAsync();
        return ruleSet is null ? NotFound() : RuleSetDetailDto.FromDomain(ruleSet);
    }

    [HttpPost]
    public async Task<ActionResult<RuleSetDetailDto>> Create(CreateRuleSetRequest request)
    {
        if (!ActorHeader.TryGet(Request, out var actor))
        {
            return BadRequest($"Header '{ActorHeader.HeaderName}' is required.");
        }

        var rules = request.Rules.Select(r => r.ToDomain()).ToList();
        var id = await _ruleSets.CreateAsync(request.Name, rules, actor, request.Reason);
        var created = await _ruleSets.GetAsync(id);
        return CreatedAtAction(nameof(Get), new { id }, RuleSetDetailDto.FromDomain(created!));
    }

    [HttpPut("{id:int}/rules")]
    public async Task<ActionResult> ReplaceRules(int id, ReplaceRulesRequest request)
    {
        if (!ActorHeader.TryGet(Request, out var actor))
        {
            return BadRequest($"Header '{ActorHeader.HeaderName}' is required.");
        }

        try
        {
            var rules = request.Rules.Select(r => r.ToDomain()).ToList();
            await _ruleSets.ReplaceRulesAsync(id, rules, actor, request.Reason);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPost("{id:int}/activate")]
    public async Task<ActionResult> Activate(int id, ActivateRuleSetRequest request)
    {
        if (!ActorHeader.TryGet(Request, out var actor))
        {
            return BadRequest($"Header '{ActorHeader.HeaderName}' is required.");
        }

        try
        {
            await _ruleSets.ActivateAsync(id, actor, request.Reason);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
