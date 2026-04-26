namespace StudyApp.Services;

public sealed class NoOpReminderNotificationService : IReminderNotificationService
{
    public Task<bool> RequestPermissionIfNeededAsync() => Task.FromResult(true);

    public Task<bool> ShowLoginReminderAsync() => Task.FromResult(false);

    public Task<bool> ShowLateReminderAsync() => Task.FromResult(false);

    public Task ScheduleLateReminderAsync(DateTime triggerAt) => Task.CompletedTask;

    public Task CancelScheduledLateReminderAsync() => Task.CompletedTask;

    public Task DismissReminderAsync() => Task.CompletedTask;
}
