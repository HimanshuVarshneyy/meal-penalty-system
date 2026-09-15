using System.Globalization;
using MealPenalty.Domain;

namespace MealPenalty.Application.PenaltyTypes;

/// <summary>AMOUNT: value is a flat dollar amount per trigger.</summary>
public class FlatAmountHandler : IPenaltyTypeHandler
{
    public PenaltyType PenaltyType => PenaltyType.Amount;

    public decimal PriceSingleTrigger(PenaltyRule rule, decimal hourlyRate) => rule.Value;

    public string Describe(PenaltyRule rule, int triggerCount, decimal perTriggerAmount, decimal totalAmount) =>
        string.Create(CultureInfo.InvariantCulture,
            $"AMOUNT rule ({rule.StartHours}h-{rule.EndHours}h): {triggerCount} x ${perTriggerAmount:F2} = ${totalAmount:F2}");
}
