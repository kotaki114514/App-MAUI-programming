namespace StudyApp.Services;

public static class ReminderPreferenceKeys
{
    public static string GetLateReminderDateKey(string userId) => $"studyapp.late-reminder-date.{userId}";
}
