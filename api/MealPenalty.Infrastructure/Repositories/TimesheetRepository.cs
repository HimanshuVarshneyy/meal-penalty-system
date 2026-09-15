using Dapper;
using MealPenalty.Application.Abstractions;
using MealPenalty.Domain;
using MealPenalty.Infrastructure.Db;

namespace MealPenalty.Infrastructure.Repositories;

public class TimesheetRepository : ITimesheetRepository
{
    private readonly SqliteConnectionFactory _factory;

    public TimesheetRepository(SqliteConnectionFactory factory) => _factory = factory;

    public async Task<int> InsertEntryAsync(TimesheetEntry entry)
    {
        using var conn = _factory.CreateOpenConnection();
        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync(
            """
            INSERT INTO TimesheetEntry (Label, WorkDate, InHours, OutHours, HourlyRate, CreatedAt)
            VALUES (@Label, @WorkDate, @InHours, @OutHours, @HourlyRate, @CreatedAt)
            """,
            new
            {
                entry.Label,
                WorkDate = entry.WorkDate.ToString("O"),
                entry.InHours,
                entry.OutHours,
                entry.HourlyRate,
                CreatedAt = DateTime.UtcNow.ToString("O")
            },
            tx);

        var id = await conn.ExecuteScalarAsync<long>("SELECT last_insert_rowid()", transaction: tx);

        foreach (var meal in entry.MealBreaks)
        {
            await conn.ExecuteAsync(
                """
                INSERT INTO TimesheetMeal (TimesheetEntryId, SeqNo, MealStart, MealEnd)
                VALUES (@TimesheetEntryId, @SeqNo, @MealStart, @MealEnd)
                """,
                new { TimesheetEntryId = id, meal.SeqNo, meal.MealStart, meal.MealEnd },
                tx);
        }

        tx.Commit();
        return (int)id;
    }
}
