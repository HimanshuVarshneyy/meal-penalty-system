using MealPenalty.Domain;

namespace MealPenalty.Api.Dtos;

public class RuleRowDto
{
    public int? Id { get; set; }
    public decimal StartHours { get; set; }
    public decimal EndHours { get; set; }
    public string PenaltyType { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal? IntervalHours { get; set; }

    public PenaltyRule ToDomain() => new()
    {
        Id = Id ?? 0,
        StartHours = StartHours,
        EndHours = EndHours,
        PenaltyType = Enum.Parse<PenaltyType>(PenaltyType, ignoreCase: true),
        Value = Value,
        IntervalHours = IntervalHours
    };

    public static RuleRowDto FromDomain(PenaltyRule rule) => new()
    {
        Id = rule.Id,
        StartHours = rule.StartHours,
        EndHours = rule.EndHours,
        PenaltyType = rule.PenaltyType.ToString(),
        Value = rule.Value,
        IntervalHours = rule.IntervalHours
    };
}

public class RuleSetSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    public static RuleSetSummaryDto FromDomain(PenaltyRuleSet rs) => new()
    {
        Id = rs.Id,
        Name = rs.Name,
        IsActive = rs.IsActive,
        EffectiveFrom = rs.EffectiveFrom,
        CreatedAt = rs.CreatedAt,
        CreatedBy = rs.CreatedBy
    };
}

public class RuleSetDetailDto : RuleSetSummaryDto
{
    public List<RuleRowDto> Rules { get; set; } = [];

    public static new RuleSetDetailDto FromDomain(PenaltyRuleSet rs) => new()
    {
        Id = rs.Id,
        Name = rs.Name,
        IsActive = rs.IsActive,
        EffectiveFrom = rs.EffectiveFrom,
        CreatedAt = rs.CreatedAt,
        CreatedBy = rs.CreatedBy,
        Rules = rs.Rules.Select(RuleRowDto.FromDomain).ToList()
    };
}

public class CreateRuleSetRequest
{
    public string Name { get; set; } = string.Empty;
    public List<RuleRowDto> Rules { get; set; } = [];
    public string? Reason { get; set; }
}

public class ReplaceRulesRequest
{
    public List<RuleRowDto> Rules { get; set; } = [];
    public string? Reason { get; set; }
}

public class ActivateRuleSetRequest
{
    public string? Reason { get; set; }
}
