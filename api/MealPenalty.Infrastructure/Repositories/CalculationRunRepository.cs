using System.Globalization;
using System.Text;
using Dapper;
using MealPenalty.Application.Abstractions;
using MealPenalty.Domain;
using MealPenalty.Infrastructure.Db;

namespace MealPenalty.Infrastructure.Repositories;

public class CalculationRunRepository : ICalculationRunRepository
{
    private readonly SqliteConnectionFactory _factory;

    public CalculationRunRepository(SqliteConnectionFactory factory) => _factory = factory;

    // SQLite's ALTER TABLE ADD COLUMN always appends new columns physically at the end of the
    // table, regardless of where they're declared in CREATE TABLE - so on a file created before
    // HourlyRate/TotalPaidAmount existed, `SELECT *` would return them last, not in this record's
    // declared order, and Dapper's record materialization matches columns positionally. Selecting
    // explicit column lists (matching these records exactly) sidesteps that entirely.
    private const string RunColumns =
        "Id, TimesheetEntryId, RuleSetId, Label, WorkDate, HourlyRate, PaidHours, TotalPenalty, TotalPaidAmount, CreatedAt, CreatedBy";

    private sealed record RunRow(long Id, long TimesheetEntryId, long RuleSetId, string Label, string WorkDate, double HourlyRate, double PaidHours, double TotalPenalty, double TotalPaidAmount, string CreatedAt, string CreatedBy);
    private sealed record DetailRow(long Id, long CalculationRunId, long WindowIndex, double WindowStart, double WindowEnd, long? RuleId, long TriggerCount, double PenaltyAmount, string Description);

    private static CalculationRun MapRun(RunRow r) => new()
    {
        Id = (int)r.Id,
        TimesheetEntryId = (int)r.TimesheetEntryId,
        RuleSetId = (int)r.RuleSetId,
        Label = r.Label,
        WorkDate = DateOnly.Parse(r.WorkDate, CultureInfo.InvariantCulture),
        HourlyRate = (decimal)r.HourlyRate,
        PaidHours = (decimal)r.PaidHours,
        TotalPenalty = (decimal)r.TotalPenalty,
        TotalPaidAmount = (decimal)r.TotalPaidAmount,
        CreatedAt = DateTime.Parse(r.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        CreatedBy = r.CreatedBy
    };

    private static CalculationRunDetail MapDetail(DetailRow r) => new()
    {
        Id = (int)r.Id,
        CalculationRunId = (int)r.CalculationRunId,
        WindowIndex = (int)r.WindowIndex,
        WindowStart = (decimal)r.WindowStart,
        WindowEnd = (decimal)r.WindowEnd,
        RuleId = r.RuleId.HasValue ? (int)r.RuleId.Value : null,
        TriggerCount = (int)r.TriggerCount,
        PenaltyAmount = (decimal)r.PenaltyAmount,
        Description = r.Description
    };

    public async Task<int> InsertRunAsync(CalculationRun run)
    {
        using var conn = _factory.CreateOpenConnection();
        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync(
            """
            INSERT INTO CalculationRun (TimesheetEntryId, RuleSetId, Label, WorkDate, HourlyRate, PaidHours, TotalPenalty, TotalPaidAmount, CreatedAt, CreatedBy)
            VALUES (@TimesheetEntryId, @RuleSetId, @Label, @WorkDate, @HourlyRate, @PaidHours, @TotalPenalty, @TotalPaidAmount, @CreatedAt, @CreatedBy)
            """,
            new
            {
                run.TimesheetEntryId,
                run.RuleSetId,
                run.Label,
                WorkDate = run.WorkDate.ToString("O"),
                run.HourlyRate,
                run.PaidHours,
                run.TotalPenalty,
                run.TotalPaidAmount,
                CreatedAt = run.CreatedAt.ToString("O"),
                run.CreatedBy
            },
            tx);

        var id = await conn.ExecuteScalarAsync<long>("SELECT last_insert_rowid()", transaction: tx);

        foreach (var detail in run.Details)
        {
            await conn.ExecuteAsync(
                """
                INSERT INTO CalculationRunDetail (CalculationRunId, WindowIndex, WindowStart, WindowEnd, RuleId, TriggerCount, PenaltyAmount, Description)
                VALUES (@CalculationRunId, @WindowIndex, @WindowStart, @WindowEnd, @RuleId, @TriggerCount, @PenaltyAmount, @Description)
                """,
                new
                {
                    CalculationRunId = id,
                    detail.WindowIndex,
                    detail.WindowStart,
                    detail.WindowEnd,
                    detail.RuleId,
                    detail.TriggerCount,
                    detail.PenaltyAmount,
                    detail.Description
                },
                tx);
        }

        tx.Commit();
        return (int)id;
    }

    public async Task<IReadOnlyList<CalculationRun>> ListRunsAsync(DateOnly? from, DateOnly? to, string? label)
    {
        using var conn = _factory.CreateOpenConnection();

        var sql = new StringBuilder($"SELECT {RunColumns} FROM CalculationRun WHERE 1=1");
        var parameters = new DynamicParameters();

        if (from.HasValue)
        {
            sql.Append(" AND WorkDate >= @from");
            parameters.Add("from", from.Value.ToString("O"));
        }

        if (to.HasValue)
        {
            sql.Append(" AND WorkDate <= @to");
            parameters.Add("to", to.Value.ToString("O"));
        }

        if (!string.IsNullOrWhiteSpace(label))
        {
            sql.Append(" AND Label LIKE @label");
            parameters.Add("label", $"%{label}%");
        }

        sql.Append(" ORDER BY WorkDate DESC, Id DESC");

        var rows = await conn.QueryAsync<RunRow>(sql.ToString(), parameters);
        return rows.Select(MapRun).ToList();
    }

    public async Task<CalculationRun?> GetByIdAsync(int id)
    {
        using var conn = _factory.CreateOpenConnection();

        var row = await conn.QuerySingleOrDefaultAsync<RunRow>($"SELECT {RunColumns} FROM CalculationRun WHERE Id=@id", new { id });
        if (row is null)
        {
            return null;
        }

        var run = MapRun(row);
        var detailRows = await conn.QueryAsync<DetailRow>(
            "SELECT * FROM CalculationRunDetail WHERE CalculationRunId=@id ORDER BY WindowIndex", new { id });
        run.Details = detailRows.Select(MapDetail).ToList();
        return run;
    }
}
