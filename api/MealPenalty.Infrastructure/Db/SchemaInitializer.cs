using Microsoft.Data.Sqlite;

namespace MealPenalty.Infrastructure.Db;

public static class SchemaInitializer
{
    private const string Schema = """
        CREATE TABLE IF NOT EXISTS PenaltyRuleSet (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            IsActive INTEGER NOT NULL DEFAULT 0,
            EffectiveFrom TEXT NOT NULL,
            CreatedAt TEXT NOT NULL,
            CreatedBy TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS PenaltyRule (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            RuleSetId INTEGER NOT NULL REFERENCES PenaltyRuleSet(Id),
            RowOrder INTEGER NOT NULL,
            StartHours REAL NOT NULL,
            EndHours REAL NOT NULL,
            PenaltyType TEXT NOT NULL,
            Value REAL NOT NULL,
            IntervalHours REAL NULL
        );

        CREATE TABLE IF NOT EXISTS PenaltyRuleAudit (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            RuleSetId INTEGER NOT NULL,
            RuleId INTEGER NULL,
            Action TEXT NOT NULL,
            ChangedBy TEXT NOT NULL,
            ChangedAt TEXT NOT NULL,
            OldValueJson TEXT NULL,
            NewValueJson TEXT NULL,
            Reason TEXT NULL
        );

        CREATE TABLE IF NOT EXISTS TimesheetEntry (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Label TEXT NOT NULL,
            WorkDate TEXT NOT NULL,
            InHours REAL NOT NULL,
            OutHours REAL NOT NULL,
            HourlyRate REAL NOT NULL,
            CreatedAt TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS TimesheetMeal (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            TimesheetEntryId INTEGER NOT NULL REFERENCES TimesheetEntry(Id),
            SeqNo INTEGER NOT NULL,
            MealStart REAL NOT NULL,
            MealEnd REAL NOT NULL
        );

        CREATE TABLE IF NOT EXISTS CalculationRun (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            TimesheetEntryId INTEGER NOT NULL REFERENCES TimesheetEntry(Id),
            RuleSetId INTEGER NOT NULL REFERENCES PenaltyRuleSet(Id),
            Label TEXT NOT NULL,
            WorkDate TEXT NOT NULL,
            HourlyRate REAL NOT NULL DEFAULT 0,
            PaidHours REAL NOT NULL,
            TotalPenalty REAL NOT NULL,
            TotalPaidAmount REAL NOT NULL DEFAULT 0,
            CreatedAt TEXT NOT NULL,
            CreatedBy TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS CalculationRunDetail (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            CalculationRunId INTEGER NOT NULL REFERENCES CalculationRun(Id),
            WindowIndex INTEGER NOT NULL,
            WindowStart REAL NOT NULL,
            WindowEnd REAL NOT NULL,
            RuleId INTEGER NULL,
            TriggerCount INTEGER NOT NULL,
            PenaltyAmount REAL NOT NULL,
            Description TEXT NOT NULL
        );
        """;

    private const string Seed = """
        INSERT INTO PenaltyRuleSet (Id, Name, IsActive, EffectiveFrom, CreatedAt, CreatedBy)
        VALUES (1, 'Sample Meal Penalty Rules', 1, @now, @now, 'system');

        INSERT INTO PenaltyRule (RuleSetId, RowOrder, StartHours, EndHours, PenaltyType, Value, IntervalHours) VALUES
            (1, 1, 6, 7, 'Amount', 25, NULL),
            (1, 2, 7, 9, 'Hour', 0.5, 0.5),
            (1, 3, 9, 24, 'Daily', 10, 1);
        """;

    public static void Initialize(SqliteConnection connection)
    {
        using var createCmd = connection.CreateCommand();
        createCmd.CommandText = Schema;
        createCmd.ExecuteNonQuery();

        // Lightweight migration: CREATE TABLE IF NOT EXISTS leaves an existing file's schema
        // untouched, so a column added after the file was first created needs an explicit ALTER.
        EnsureColumn(connection, "CalculationRun", "HourlyRate", "REAL NOT NULL DEFAULT 0");
        EnsureColumn(connection, "CalculationRun", "TotalPaidAmount", "REAL NOT NULL DEFAULT 0");

        using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(*) FROM PenaltyRuleSet";
        var existing = (long)checkCmd.ExecuteScalar()!;
        if (existing > 0)
        {
            return;
        }

        using var seedCmd = connection.CreateCommand();
        seedCmd.CommandText = Seed;
        seedCmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("O"));
        seedCmd.ExecuteNonQuery();
    }

    private static void EnsureColumn(SqliteConnection connection, string table, string column, string columnDefinition)
    {
        using var pragmaCmd = connection.CreateCommand();
        pragmaCmd.CommandText = $"PRAGMA table_info({table})";
        using var reader = pragmaCmd.ExecuteReader();
        var exists = false;
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
            {
                exists = true;
                break;
            }
        }
        reader.Close();

        if (exists)
        {
            return;
        }

        using var alterCmd = connection.CreateCommand();
        alterCmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {columnDefinition}";
        alterCmd.ExecuteNonQuery();
    }
}
