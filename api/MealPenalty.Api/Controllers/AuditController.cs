using MealPenalty.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace MealPenalty.Api.Controllers;

[ApiController]
[Route("api/v1/audit")]
public class AuditController : ControllerBase
{
    private readonly IAuditRepository _audit;

    public AuditController(IAuditRepository audit) => _audit = audit;

    [HttpGet("rules")]
    public async Task<ActionResult> ListRuleAudit(
        [FromQuery] int? ruleSetId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var entries = await _audit.ListAsync(ruleSetId, from, to);
        return Ok(entries);
    }
}
