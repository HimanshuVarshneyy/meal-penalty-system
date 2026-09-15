using MealPenalty.Domain;

namespace MealPenalty.Application.Engine;

public interface IPenaltyCalculationEngine
{
    DayResult Calculate(TimesheetEntry entry, PenaltyRuleSet ruleSet);
}
