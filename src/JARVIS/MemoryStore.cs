using Microsoft.Data.Sqlite;

namespace Jarvis;

// SQLite at %LOCALAPPDATA%\JARVIS\memory.db: conversation history and the tool audit log.
public sealed class MemoryStore(string? dbPath = null)
{
    private readonly string _dbPath = dbPath ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JARVIS", "memory.db");

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
        await ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS conversation (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                role TEXT NOT NULL,
                content TEXT NOT NULL,
                created_utc TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS tool_audit (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                tool TEXT NOT NULL,
                arguments TEXT NOT NULL,
                approved INTEGER NOT NULL,
                result TEXT NOT NULL,
                created_utc TEXT NOT NULL
            );
            """);
    }

    public Task SaveAsync(string role, string content) =>
        ExecuteAsync("INSERT INTO conversation(role, content, created_utc) VALUES($role, $content, $utc)",
            ("$role", role), ("$content", content), ("$utc", DateTime.UtcNow.ToString("O")));

    /// <summary>Records every tool request, whether it ran or was declined.</summary>
    public Task LogToolAsync(string tool, string arguments, bool approved, string result) =>
        ExecuteAsync("INSERT INTO tool_audit(tool, arguments, approved, result, created_utc) VALUES($tool, $args, $approved, $result, $utc)",
            ("$tool", tool), ("$args", arguments), ("$approved", approved), ("$result", result), ("$utc", DateTime.UtcNow.ToString("O")));

    public async Task<List<ChatLine>> GetRecentAsync(int count)
    {
        var result = new List<ChatLine>();
        await using var connection = await OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT role, content FROM conversation ORDER BY id DESC LIMIT $count";
        command.Parameters.AddWithValue("$count", count);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(new ChatLine(reader.GetString(0), reader.GetString(1)));
        result.Reverse();
        return result;
    }

    private async Task ExecuteAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = await OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<SqliteConnection> OpenAsync()
    {
        var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();
        return connection;
    }
}
