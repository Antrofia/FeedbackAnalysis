using FeedbackAnalysis.DataApi.Context;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FeedbackAnalysis.Tests.TestSupport;

/// <summary>
/// SQLite in-memory база с открытый соединением на время жизни экземпляра.
/// EFContext сам вызывает EnsureCreated() в конструкторе — схема создаётся автоматически.
/// </summary>
public sealed class SqliteTestDb : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteTestDb()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    public EFContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EFContext>()
            .UseSqlite(_connection)
            .Options;

        return new EFContext(options);
    }

    public void Dispose() => _connection.Dispose();
}
