namespace StudyApp.Services;

public interface IReminderNotificationService
{
    Task<bool> RequestPermissionIfNeededAsync();

    Task<bool> ShowLoginReminderAsync();

    Task<bool> ShowLateReminderAsync();

    Task ScheduleLateReminderAsync(DateTime triggerAt);

    Task CancelScheduledLateReminderAsync();

    Task DismissReminderAsync();
}
