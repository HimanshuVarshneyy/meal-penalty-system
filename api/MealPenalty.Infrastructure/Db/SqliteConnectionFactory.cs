using Microsoft.Data.Sqlite;

namespace MealPenalty.Infrastructure.Db;

/// <summary>
/// Every call opens a fresh connection to the given SQLite database, so Dapper repositories
/// behave exactly like they would against a real database (open/use/dispose per call). When
/// pointed at an in-memory, shared-cache connection string a keep-alive connection is held open
/// for the process lifetime, because SQLite drops that kind of database the moment its last
/// connection closes - a file-backed connection string needs no such trick, since the file itself
/// is the persistence.
/// </summary>
public class SqliteConnectionFactory : IDisposable
{
    private readonly string _connectionString;
    private readonly SqliteConnection? _keepAlive;

    public SqliteConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;

        if (connectionString.Contains("Mode=Memory", StringComparison.OrdinalIgnoreCase))
        {
            _keepAlive = new SqliteConnection(connectionString);
            _keepAlive.Open();
        }
    }

    public SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    public void Dispose()
    {
        _keepAlive?.Dispose();
        GC.SuppressFinalize(this);
    }
}
