using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using StudyApp.Models;

namespace StudyApp.Services;

internal static class SqliteAppDatabaseStore
{
    public static void EnsureCreated(string databasePath)
    {
        using var connection = OpenConnection(databasePath);
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS AppState (
                Key TEXT PRIMARY KEY,
                Value TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS Users (
                Id TEXT PRIMARY KEY,
                UserName TEXT NOT NULL,
                Password TEXT NOT NULL,
                Nickname TEXT NOT NULL,
                AvatarEmoji TEXT NOT NULL,
                AvatarColorHex TEXT NOT NULL,
                AvatarImageBase64 TEXT NULL,
                RegisteredAt TEXT NOT NULL,
                CheckInDatesJson TEXT NOT NULL,
                CurrentStreak INTEGER NOT NULL,
                BestStreak INTEGER NOT NULL,
                LastCheckInAt TEXT NULL,
                UnlockedAchievementDaysJson TEXT NOT NULL,
                LastReminderAt TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS Notes (
                Id TEXT PRIMARY KEY,
                UserId TEXT NOT NULL,
                Title TEXT NOT NULL,
                Content TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_Notes_UserId ON Notes(UserId);
            """;
        command.ExecuteNonQuery();
    }

    public static void ImportLegacyJsonIfNeeded(string databasePath, string legacyJsonPath, JsonSerializerOptions jsonOptions)
    {
        if (!File.Exists(legacyJsonPath))
        {
            return;
        }

        using var connection = OpenConnection(databasePath);
        if (!IsEmpty(connection))
        {
            return;
        }

        // Only seed from the old JSON file when the SQLite database is still empty.
        using var stream = File.OpenRead(legacyJsonPath);
        var database = JsonSerializer.Deserialize<AppDatabase>(stream, jsonOptions) ?? new AppDatabase();
        Save(connection, database, jsonOptions);
    }

    public static AppDatabase Load(string databasePath, JsonSerializerOptions jsonOptions)
    {
        using var connection = OpenConnection(databasePath);
        return Load(connection, jsonOptions);
    }

    public static void Save(string databasePath, AppDatabase database, JsonSerializerOptions jsonOptions)
    {
        using var connection = OpenConnection(databasePath);
        Save(connection, database, jsonOptions);
    }

    private static AppDatabase Load(SqliteConnection connection, JsonSerializerOptions jsonOptions)
    {
        var database = new AppDatabase
        {
            CurrentUserId = ReadCurrentUserId(connection)
        };

        // Rehydrate users into the existing domain model expected by UserSessionService.
        using (var userCommand = connection.CreateCommand())
        {
            userCommand.CommandText =
                """
                SELECT Id, UserName, Password, Nickname, AvatarEmoji, AvatarColorHex, AvatarImageBase64,
                       RegisteredAt, CheckInDatesJson, CurrentStreak, BestStreak, LastCheckInAt,
                       UnlockedAchievementDaysJson, LastReminderAt
                FROM Users;
                """;

            using var reader = userCommand.ExecuteReader();
            while (reader.Read())
            {
                database.Users.Add(new UserRecord
                {
                    Id = reader.GetString(0),
                    UserName = reader.GetString(1),
                    Password = reader.GetString(2),
                    Nickname = reader.GetString(3),
                    AvatarEmoji = reader.GetString(4),
                    AvatarColorHex = reader.GetString(5),
                    AvatarImageBase64 = reader.IsDBNull(6) ? null : reader.GetString(6),
                    RegisteredAt = ReadDateTime(reader.GetString(7)),
                    CheckInDates = DeserializeList<string>(reader.GetString(8), jsonOptions),
                    CurrentStreak = reader.GetInt32(9),
                    BestStreak = reader.GetInt32(10),
                    LastCheckInAt = reader.IsDBNull(11) ? null : ReadNullableDateTime(reader.GetString(11)),
                    UnlockedAchievementDays = DeserializeList<int>(reader.GetString(12), jsonOptions),
                    LastReminderAt = reader.IsDBNull(13) ? null : ReadNullableDateTime(reader.GetString(13))
                });
            }
        }

        using (var noteCommand = connection.CreateCommand())
        {
            noteCommand.CommandText =
                """
                SELECT Id, UserId, Title, Content, CreatedAt
                FROM Notes;
                """;

            using var reader = noteCommand.ExecuteReader();
            while (reader.Read())
            {
                database.Notes.Add(new NoteRecord
                {
                    Id = reader.GetString(0),
                    UserId = reader.GetString(1),
                    Title = reader.GetString(2),
                    Content = reader.GetString(3),
                    CreatedAt = ReadDateTime(reader.GetString(4))
                });
            }
        }

        return database;
    }

    private static void Save(SqliteConnection connection, AppDatabase database, JsonSerializerOptions jsonOptions)
    {
        using var transaction = connection.BeginTransaction();

        // Replace the snapshot atomically so app code can continue to treat storage as a full document.
        ExecuteNonQuery(connection, transaction, "DELETE FROM Notes;");
        ExecuteNonQuery(connection, transaction, "DELETE FROM Users;");
        ExecuteNonQuery(connection, transaction, "DELETE FROM AppState;");

        if (!string.IsNullOrWhiteSpace(database.CurrentUserId))
        {
            using var stateCommand = connection.CreateCommand();
            stateCommand.Transaction = transaction;
            stateCommand.CommandText = "INSERT INTO AppState (Key, Value) VALUES ($key, $value);";
            stateCommand.Parameters.AddWithValue("$key", DatabaseConstants.CurrentUserKey);
            stateCommand.Parameters.AddWithValue("$value", database.CurrentUserId);
            stateCommand.ExecuteNonQuery();
        }

        foreach (var user in database.Users)
        {
            using var userCommand = connection.CreateCommand();
            userCommand.Transaction = transaction;
            userCommand.CommandText =
                """
                INSERT INTO Users (
                    Id, UserName, Password, Nickname, AvatarEmoji, AvatarColorHex, AvatarImageBase64,
                    RegisteredAt, CheckInDatesJson, CurrentStreak, BestStreak, LastCheckInAt,
                    UnlockedAchievementDaysJson, LastReminderAt
                )
                VALUES (
                    $id, $userName, $password, $nickname, $avatarEmoji, $avatarColorHex, $avatarImageBase64,
                    $registeredAt, $checkInDatesJson, $currentStreak, $bestStreak, $lastCheckInAt,
                    $unlockedAchievementDaysJson, $lastReminderAt
                );
                """;

            userCommand.Parameters.AddWithValue("$id", user.Id);
            userCommand.Parameters.AddWithValue("$userName", user.UserName);
            userCommand.Parameters.AddWithValue("$password", user.Password);
            userCommand.Parameters.AddWithValue("$nickname", user.Nickname);
            userCommand.Parameters.AddWithValue("$avatarEmoji", user.AvatarEmoji);
            userCommand.Parameters.AddWithValue("$avatarColorHex", user.AvatarColorHex);
            userCommand.Parameters.AddWithValue("$avatarImageBase64", (object?)user.AvatarImageBase64 ?? DBNull.Value);
            userCommand.Parameters.AddWithValue("$registeredAt", WriteDateTime(user.RegisteredAt));
            userCommand.Parameters.AddWithValue("$checkInDatesJson", JsonSerializer.Serialize(user.CheckInDates, jsonOptions));
            userCommand.Parameters.AddWithValue("$currentStreak", user.CurrentStreak);
            userCommand.Parameters.AddWithValue("$bestStreak", user.BestStreak);
            userCommand.Parameters.AddWithValue("$lastCheckInAt", user.LastCheckInAt is null ? DBNull.Value : WriteDateTime(user.LastCheckInAt.Value));
            userCommand.Parameters.AddWithValue("$unlockedAchievementDaysJson", JsonSerializer.Serialize(user.UnlockedAchievementDays, jsonOptions));
            userCommand.Parameters.AddWithValue("$lastReminderAt", user.LastReminderAt is null ? DBNull.Value : WriteDateTime(user.LastReminderAt.Value));
            userCommand.ExecuteNonQuery();
        }

        foreach (var note in database.Notes)
        {
            using var noteCommand = connection.CreateCommand();
            noteCommand.Transaction = transaction;
            noteCommand.CommandText =
                """
                INSERT INTO Notes (Id, UserId, Title, Content, CreatedAt)
                VALUES ($id, $userId, $title, $content, $createdAt);
                """;

            noteCommand.Parameters.AddWithValue("$id", note.Id);
            noteCommand.Parameters.AddWithValue("$userId", note.UserId);
            noteCommand.Parameters.AddWithValue("$title", note.Title);
            noteCommand.Parameters.AddWithValue("$content", note.Content);
            noteCommand.Parameters.AddWithValue("$createdAt", WriteDateTime(note.CreatedAt));
            noteCommand.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    private static SqliteConnection OpenConnection(string databasePath)
    {
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // SQLitePCL needs one-time native initialization before opening connections on MAUI platforms.
        SQLitePCL.Batteries_V2.Init();

        var connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();
        return connection;
    }

    private static bool IsEmpty(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                (SELECT COUNT(*) FROM AppState) +
                (SELECT COUNT(*) FROM Users) +
                (SELECT COUNT(*) FROM Notes);
            """;
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) == 0;
    }

    private static string? ReadCurrentUserId(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Value FROM AppState WHERE Key = $key LIMIT 1;";
        command.Parameters.AddWithValue("$key", DatabaseConstants.CurrentUserKey);
        return command.ExecuteScalar() as string;
    }

    private static void ExecuteNonQuery(SqliteConnection connection, SqliteTransaction transaction, string commandText)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = commandText;
        command.ExecuteNonQuery();
    }

    private static List<T> DeserializeList<T>(string json, JsonSerializerOptions jsonOptions)
    {
        return JsonSerializer.Deserialize<List<T>>(json, jsonOptions) ?? [];
    }

    private static DateTime ReadDateTime(string value)
    {
        return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    private static DateTime? ReadNullableDateTime(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : ReadDateTime(value);
    }

    private static string WriteDateTime(DateTime value)
    {
        return value.ToString("O", CultureInfo.InvariantCulture);
    }
}
