using System.Globalization;
using System.Text;
using Dapper;
using MealPenalty.Application.Abstractions;
using MealPenalty.Domain;
using MealPenalty.Infrastructure.Db;

namespace MealPenalty.Infrastructure.Repositories;

/// <summary>Append-only: there is deliberately no Update/Delete here.</summary>
public class AuditRepository : IAuditRepository
{
    private readonly SqliteConnectionFactory _factory;

    public AuditRepository(SqliteConnectionFactory factory) => _factory = factory;

    private sealed record AuditRow(long Id, long RuleSetId, long? RuleId, string Action, string ChangedBy, string ChangedAt, string? OldValueJson, string? NewValueJson, string? Reason);

    private static PenaltyRuleAudit MapAudit(AuditRow r) => new()
    {
        Id = (int)r.Id,
        RuleSetId = (int)r.RuleSetId,
        RuleId = r.RuleId.HasValue ? (int)r.RuleId.Value : null,
        Action = Enum.Parse<AuditAction>(r.Action),
        ChangedBy = r.ChangedBy,
        ChangedAt = DateTime.Parse(r.ChangedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        OldValueJson = r.OldValueJson,
        NewValueJson = r.NewValueJson,
        Reason = r.Reason
    };

    public async Task InsertAsync(PenaltyRuleAudit audit)
    {
        using var conn = _factory.CreateOpenConnection();
        await conn.ExecuteAsync(
            """
            INSERT INTO PenaltyRuleAudit (RuleSetId, RuleId, Action, ChangedBy, ChangedAt, OldValueJson, NewValueJson, Reason)
            VALUES (@RuleSetId, @RuleId, @Action, @ChangedBy, @ChangedAt, @OldValueJson, @NewValueJson, @Reason)
            """,
            new
            {
                audit.RuleSetId,
                audit.RuleId,
                Action = audit.Action.ToString(),
                audit.ChangedBy,
                ChangedAt = audit.ChangedAt.ToString("O"),
                audit.OldValueJson,
                audit.NewValueJson,
                audit.Reason
            });
    }

    public async Task<IReadOnlyList<PenaltyRuleAudit>> ListAsync(int? ruleSetId, DateOnly? from, DateOnly? to)
    {
        using var conn = _factory.CreateOpenConnection();

        var sql = new StringBuilder("SELECT * FROM PenaltyRuleAudit WHERE 1=1");
        var parameters = new DynamicParameters();

        if (ruleSetId.HasValue)
        {
            sql.Append(" AND RuleSetId = @ruleSetId");
            parameters.Add("ruleSetId", ruleSetId.Value);
        }

        if (from.HasValue)
        {
            sql.Append(" AND ChangedAt >= @from");
            parameters.Add("from", from.Value.ToString("O"));
        }

        if (to.HasValue)
        {
            // ChangedAt carries a time component (ISO "O"), so make the bound inclusive of the whole day.
            sql.Append(" AND ChangedAt <= @to");
            parameters.Add("to", to.Value.ToDateTime(TimeOnly.MaxValue).ToString("O"));
        }

        sql.Append(" ORDER BY ChangedAt DESC, Id DESC");

        var rows = await conn.QueryAsync<AuditRow>(sql.ToString(), parameters);
        return rows.Select(MapAudit).ToList();
    }
}
