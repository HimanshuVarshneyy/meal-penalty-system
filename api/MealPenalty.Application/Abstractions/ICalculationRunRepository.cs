using MealPenalty.Domain;

namespace MealPenalty.Application.Abstractions;

public interface ICalculationRunRepository
{
    Task<int> InsertRunAsync(CalculationRun run);
    Task<IReadOnlyList<CalculationRun>> ListRunsAsync(DateOnly? from, DateOnly? to, string? label);
    Task<CalculationRun?> GetByIdAsync(int id);
}
