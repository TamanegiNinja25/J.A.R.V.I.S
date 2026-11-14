using Microsoft.Data.Sqlite;

namespace Jarvis;

public sealed class MemoryStore
{
    private readonly string _dbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JARVIS", "memory.db");
    private string ConnectionString => $"Data Source={_dbPath}";

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS conversation (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                role TEXT NOT NULL,
                content TEXT NOT NULL,
                created_utc TEXT NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync();
    }

    public async Task SaveAsync(string role, string content)
    {
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO conversation(role, content, created_utc) VALUES($role,$content,$utc)";
        command.Parameters.AddWithValue("$role", role);
        command.Parameters.AddWithValue("$content", content);
        command.Parameters.AddWithValue("$utc", DateTime.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<ChatLine>> GetRecentAsync(int count)
    {
        var result = new List<ChatLine>();
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT role, content FROM conversation ORDER BY id DESC LIMIT $count";
        command.Parameters.AddWithValue("$count", count);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(new ChatLine(reader.GetString(0), reader.GetString(1)));
        result.Reverse();
        return result;
    }
}
