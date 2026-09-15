using MealPenalty.Application.Engine;
using MealPenalty.Application.PenaltyTypes;
using MealPenalty.Application.Services;
using MealPenalty.Application.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace MealPenalty.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddMealPenaltyApplication(this IServiceCollection services)
    {
        services.AddSingleton<IWindowSplitter, DefaultWindowSplitter>();
        services.AddSingleton<IPenaltyTypeHandler, HourRateHandler>();
        services.AddSingleton<IPenaltyTypeHandler, FlatAmountHandler>();
        services.AddSingleton<IPenaltyTypeHandler, DailyPercentHandler>();
        services.AddSingleton<IPenaltyCalculationEngine, PenaltyCalculationEngine>();
        services.AddScoped<RuleSetService>();
        services.AddScoped<SimulationService>();
        return services;
    }
}
