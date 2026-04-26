using System.Text.Json;
using StudyApp.Models;

namespace StudyApp.Services;

public sealed class DatabaseService
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private bool _initialized;

    public string DatabasePath => DatabaseConstants.GetSqlitePath(FileSystem.AppDataDirectory);

    public string LegacyJsonPath => DatabaseConstants.GetLegacyJsonPath(FileSystem.AppDataDirectory);

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        await _gate.WaitAsync();
        try
        {
            if (_initialized)
            {
                return;
            }

            // Create the SQLite schema first, then import legacy JSON data only once.
            SqliteAppDatabaseStore.EnsureCreated(DatabasePath);
            SqliteAppDatabaseStore.ImportLegacyJsonIfNeeded(DatabasePath, LegacyJsonPath, _jsonOptions);
            _initialized = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<TResult> ReadAsync<TResult>(Func<AppDatabase, TResult> reader)
    {
        await InitializeAsync();

        await _gate.WaitAsync();
        try
        {
            // Keep the rest of the app working against the same in-memory snapshot shape.
            var database = SqliteAppDatabaseStore.Load(DatabasePath, _jsonOptions);
            return reader(database);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<TResult> WriteAsync<TResult>(Func<AppDatabase, TResult> writer)
    {
        await InitializeAsync();

        await _gate.WaitAsync();
        try
        {
            // Load-modify-save preserves the existing service API while switching persistence to SQLite.
            var database = SqliteAppDatabaseStore.Load(DatabasePath, _jsonOptions);
            var result = writer(database);
            SqliteAppDatabaseStore.Save(DatabasePath, database, _jsonOptions);
            return result;
        }
        finally
        {
            _gate.Release();
        }
    }
}
