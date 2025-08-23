using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

public class DbInitializer
{
    private readonly string _connString;
    public DbInitializer(IConfiguration cfg) => _connString = cfg.GetConnectionString("Sqlite")!;

    public async Task EnsureCreatedAsync()
    {
        await using var conn = new SqliteConnection(_connString);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Jokes (
              Id INTEGER PRIMARY KEY AUTOINCREMENT,
              Text TEXT NOT NULL,
              Source TEXT NOT NULL,
              CreatedAtUtc TEXT NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS UX_Jokes_Text ON Jokes(Text);";
        await cmd.ExecuteNonQueryAsync();
    }
}
