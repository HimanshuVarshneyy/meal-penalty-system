using MealPenalty.Application.Abstractions;
using MealPenalty.Infrastructure.Db;
using MealPenalty.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace MealPenalty.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddMealPenaltyInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton(_ => new SqliteConnectionFactory(connectionString));
        services.AddSingleton<IRuleSetRepository, RuleSetRepository>();
        services.AddSingleton<ITimesheetRepository, TimesheetRepository>();
        services.AddSingleton<ICalculationRunRepository, CalculationRunRepository>();
        services.AddSingleton<IAuditRepository, AuditRepository>();
        return services;
    }

    public static void InitializeMealPenaltyDatabase(this IServiceProvider services)
    {
        var factory = services.GetRequiredService<SqliteConnectionFactory>();
        using var conn = factory.CreateOpenConnection();
        SchemaInitializer.Initialize(conn);
    }
}
