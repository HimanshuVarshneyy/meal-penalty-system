using System.Globalization;
using MealPenalty.Domain;

namespace MealPenalty.Application.PenaltyTypes;

/// <summary>HOUR: value is a number of hours, priced at the crew member's hourly rate.</summary>
public class HourRateHandler : IPenaltyTypeHandler
{
    public PenaltyType PenaltyType => PenaltyType.Hour;

    public decimal PriceSingleTrigger(PenaltyRule rule, decimal hourlyRate) => rule.Value * hourlyRate;

    public string Describe(PenaltyRule rule, int triggerCount, decimal perTriggerAmount, decimal totalAmount) =>
        string.Create(CultureInfo.InvariantCulture,
            $"HOUR rule ({rule.StartHours}h-{rule.EndHours}h): {triggerCount} x ({rule.Value}h x rate) = ${perTriggerAmount:F2} each = ${totalAmount:F2}");
}
