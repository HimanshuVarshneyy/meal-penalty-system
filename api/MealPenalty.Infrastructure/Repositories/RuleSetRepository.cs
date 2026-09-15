using System.Globalization;
using Dapper;
using MealPenalty.Application.Abstractions;
using MealPenalty.Domain;
using MealPenalty.Infrastructure.Db;
using Microsoft.Data.Sqlite;

namespace MealPenalty.Infrastructure.Repositories;

/// <summary>
/// Rows are mapped to plain records using the CLR types Microsoft.Data.Sqlite actually returns
/// (long/string/double), then converted to rich domain types by hand. Dapper's generated
/// deserializer unboxes column values directly into the target property type, so asking it to
/// map straight onto decimal/bool/enum properties against SQLite's INTEGER/REAL/TEXT storage
/// throws InvalidCastException at runtime - going through primitive rows sidesteps that entirely.
/// </summary>
public class RuleSetRepository : IRuleSetRepository
{
    private readonly SqliteConnectionFactory _factory;

    public RuleSetRepository(SqliteConnectionFactory factory) => _factory = factory;

    private sealed record RuleSetRow(long Id, string Name, long IsActive, string EffectiveFrom, string CreatedAt, string CreatedBy);
    private sealed record RuleRow(long Id, long RuleSetId, long RowOrder, double StartHours, double EndHours, string PenaltyType, double Value, double? IntervalHours);

    private static PenaltyRuleSet MapRuleSet(RuleSetRow r) => new()
    {
        Id = (int)r.Id,
        Name = r.Name,
        IsActive = r.IsActive != 0,
        EffectiveFrom = DateTime.Parse(r.EffectiveFrom, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        CreatedAt = DateTime.Parse(r.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        CreatedBy = r.CreatedBy
    };

    private static PenaltyRule MapRule(RuleRow r) => new()
    {
        Id = (int)r.Id,
        RuleSetId = (int)r.RuleSetId,
        RowOrder = (int)r.RowOrder,
        StartHours = (decimal)r.StartHours,
        EndHours = (decimal)r.EndHours,
        PenaltyType = Enum.Parse<PenaltyType>(r.PenaltyType),
        Value = (decimal)r.Value,
        IntervalHours = r.IntervalHours.HasValue ? (decimal)r.IntervalHours.Value : null
    };

    public async Task<IReadOnlyList<PenaltyRuleSet>> ListSetsAsync()
    {
        using var conn = _factory.CreateOpenConnection();
        var rows = await conn.QueryAsync<RuleSetRow>("SELECT * FROM PenaltyRuleSet ORDER BY Id DESC");
        return rows.Select(MapRuleSet).ToList();
    }

    public async Task<PenaltyRuleSet?> GetByIdAsync(int id)
    {
        using var conn = _factory.CreateOpenConnection();
        return await LoadWithRulesAsync(conn, null, id);
    }

    public async Task<PenaltyRuleSet?> GetActiveAsync()
    {
        using var conn = _factory.CreateOpenConnection();
        return await LoadWithRulesAsync(conn, null, null);
    }

    private static async Task<PenaltyRuleSet?> LoadWithRulesAsync(SqliteConnection conn, SqliteTransaction? tx, int? id)
    {
        var row = id.HasValue
            ? await conn.QuerySingleOrDefaultAsync<RuleSetRow>("SELECT * FROM PenaltyRuleSet WHERE Id=@id", new { id }, tx)
            : await conn.QuerySingleOrDefaultAsync<RuleSetRow>("SELECT * FROM PenaltyRuleSet WHERE IsActive=1 LIMIT 1", transaction: tx);

        if (row is null)
        {
            return null;
        }

        var ruleSet = MapRuleSet(row);
        var ruleRows = await conn.QueryAsync<RuleRow>(
            "SELECT * FROM PenaltyRule WHERE RuleSetId=@id ORDER BY RowOrder", new { id = row.Id }, tx);
        ruleSet.Rules = ruleRows.Select(MapRule).ToList();
        return ruleSet;
    }

    public async Task<int> InsertRuleSetAsync(PenaltyRuleSet ruleSet)
    {
        using var conn = _factory.CreateOpenConnection();
        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync(
            """
            INSERT INTO PenaltyRuleSet (Name, IsActive, EffectiveFrom, CreatedAt, CreatedBy)
            VALUES (@Name, @IsActive, @EffectiveFrom, @CreatedAt, @CreatedBy)
            """,
            new
            {
                ruleSet.Name,
                IsActive = ruleSet.IsActive ? 1 : 0,
                EffectiveFrom = ruleSet.EffectiveFrom.ToString("O"),
                CreatedAt = ruleSet.CreatedAt.ToString("O"),
                ruleSet.CreatedBy
            },
            tx);

        var id = await conn.ExecuteScalarAsync<long>("SELECT last_insert_rowid()", transaction: tx);
        await InsertRulesAsync(conn, tx, (int)id, ruleSet.Rules);

        tx.Commit();
        return (int)id;
    }

    public async Task ReplaceRulesAsync(int ruleSetId, IReadOnlyList<PenaltyRule> rules)
    {
        using var conn = _factory.CreateOpenConnection();
        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync("DELETE FROM PenaltyRule WHERE RuleSetId=@ruleSetId", new { ruleSetId }, tx);
        await InsertRulesAsync(conn, tx, ruleSetId, rules);

        tx.Commit();
    }

    public async Task ActivateAsync(int ruleSetId)
    {
        using var conn = _factory.CreateOpenConnection();
        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync("UPDATE PenaltyRuleSet SET IsActive=0", transaction: tx);
        await conn.ExecuteAsync("UPDATE PenaltyRuleSet SET IsActive=1 WHERE Id=@ruleSetId", new { ruleSetId }, tx);

        tx.Commit();
    }

    private static async Task InsertRulesAsync(SqliteConnection conn, SqliteTransaction tx, int ruleSetId, IReadOnlyList<PenaltyRule> rules)
    {
        var rowOrder = 1;
        foreach (var rule in rules)
        {
            await conn.ExecuteAsync(
                """
                INSERT INTO PenaltyRule (RuleSetId, RowOrder, StartHours, EndHours, PenaltyType, Value, IntervalHours)
                VALUES (@RuleSetId, @RowOrder, @StartHours, @EndHours, @PenaltyType, @Value, @IntervalHours)
                """,
                new
                {
                    RuleSetId = ruleSetId,
                    RowOrder = rowOrder,
                    rule.StartHours,
                    rule.EndHours,
                    PenaltyType = rule.PenaltyType.ToString(),
                    rule.Value,
                    rule.IntervalHours
                },
                tx);
            rowOrder++;
        }
    }
}
