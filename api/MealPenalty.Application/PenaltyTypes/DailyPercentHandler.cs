using System.Globalization;
using MealPenalty.Domain;

namespace MealPenalty.Application.PenaltyTypes;

/// <summary>DAILY: value is a percentage of daily earnings (8 x hourly rate) per trigger.</summary>
public class DailyPercentHandler : IPenaltyTypeHandler
{
    private const decimal StandardDayHours = 8m;

    public PenaltyType PenaltyType => PenaltyType.Daily;

    public decimal PriceSingleTrigger(PenaltyRule rule, decimal hourlyRate)
    {
        var dailyEarnings = StandardDayHours * hourlyRate;
        return rule.Value / 100m * dailyEarnings;
    }

    public string Describe(PenaltyRule rule, int triggerCount, decimal perTriggerAmount, decimal totalAmount) =>
        string.Create(CultureInfo.InvariantCulture,
            $"DAILY rule ({rule.StartHours}h-{rule.EndHours}h): {triggerCount} x ({rule.Value}% x daily earnings) = ${perTriggerAmount:F2} each = ${totalAmount:F2}");
}
