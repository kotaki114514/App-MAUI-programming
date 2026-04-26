namespace StudyApp.Services;

public static class DatabaseConstants
{
    public const string CurrentUserKey = "current_user_id";
    public const string LegacyJsonFileName = "database.json";
    public const string SqliteFileName = "studyapp.db";

    public static string GetLegacyJsonPath(string appDataDirectory) => Path.Combine(appDataDirectory, LegacyJsonFileName);

    public static string GetSqlitePath(string appDataDirectory) => Path.Combine(appDataDirectory, SqliteFileName);
}
