using MealPenalty.Domain;

namespace MealPenalty.Application.PenaltyTypes;

/// <summary>
/// Prices a single trigger of a rule row. The engine multiplies this by the trigger count.
/// Add a new PenaltyType by adding one implementation here and registering it - the engine
/// and window logic never need to change.
/// </summary>
public interface IPenaltyTypeHandler
{
    PenaltyType PenaltyType { get; }

    decimal PriceSingleTrigger(PenaltyRule rule, decimal hourlyRate);

    string Describe(PenaltyRule rule, int triggerCount, decimal perTriggerAmount, decimal totalAmount);
}
