using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace Aegis.Desktop.Services;

public class LocalStorageService
{
    private readonly string _dbPath;

    public LocalStorageService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var aegisFolder = Path.Combine(appDataPath, "AegisMessenger");
        Directory.CreateDirectory(aegisFolder);
        _dbPath = Path.Combine(aegisFolder, "aegis.db");

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        var createTableCommand = connection.CreateCommand();
        createTableCommand.CommandText = @"
            CREATE TABLE IF NOT EXISTS Settings (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Messages (
                Id TEXT PRIMARY KEY,
                ConversationId TEXT NOT NULL,
                Content TEXT NOT NULL,
                Timestamp TEXT NOT NULL,
                DisappearAfterSeconds INTEGER,
                ExpiresAt TEXT
            );
        ";
        createTableCommand.ExecuteNonQuery();
    }

    public async Task<string?> GetSettingAsync(string key)
    {
        await using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Value FROM Settings WHERE Key = $key";
        command.Parameters.AddWithValue("$key", key);

        var result = await command.ExecuteScalarAsync();
        return result?.ToString();
    }

    public async Task SetSettingAsync(string key, string value)
    {
        await using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT OR REPLACE INTO Settings (Key, Value)
            VALUES ($key, $value)
        ";
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<T?> GetObjectAsync<T>(string key) where T : class
    {
        var json = await GetSettingAsync(key);
        return json != null ? JsonSerializer.Deserialize<T>(json) : null;
    }

    public async Task SetObjectAsync<T>(string key, T value) where T : class
    {
        var json = JsonSerializer.Serialize(value);
        await SetSettingAsync(key, json);
    }
}
