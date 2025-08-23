using ChuckFunctionApp.Infrastructure.Storage;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Threading.Tasks;

public class JokesRepository : IJokesRepository
{
    private readonly string _connectionString;

    public JokesRepository(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
    }

    public async Task InitializeAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
        @"
            CREATE TABLE IF NOT EXISTS Jokes (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Text TEXT NOT NULL UNIQUE
            );
        ";
        await command.ExecuteNonQueryAsync();
    }

    public async Task<int> InsertJokeAsync(Joke joke)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT OR IGNORE INTO Jokes (Text, Source, CreatedAtUtc) VALUES (@text, @source, DATETIME('now'))";
        command.Parameters.AddWithValue("@text", joke.Text);
        command.Parameters.AddWithValue("@source", joke.Source);

        return await command.ExecuteNonQueryAsync();
    }
}
