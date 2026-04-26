#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Microsoft.Maui.ApplicationModel;
using StudyApp.Models;

namespace StudyApp.Services;

public sealed class ReminderNotificationService : IReminderNotificationService
{
    internal const string ChannelId = "studyapp.checkin.reminders";
    internal const int AlarmRequestCode = 2300;
    internal const int NotificationId = 2301;

    private readonly Context _context;

    public ReminderNotificationService()
    {
        _context = global::Android.App.Application.Context;
        EnsureChannel(_context);
    }

    public async Task<bool> RequestPermissionIfNeededAsync()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            return true;
        }

        var status = await Permissions.CheckStatusAsync<NotificationPermission>();
        if (status == PermissionStatus.Granted)
        {
            return true;
        }

        status = await Permissions.RequestAsync<NotificationPermission>();
        return status == PermissionStatus.Granted;
    }

    public Task<bool> ShowLoginReminderAsync()
    {
        return Task.FromResult(ShowReminder(
            _context,
            "Today's check-in is pending",
            "You have not completed today's study check-in yet. Open the app and finish it now."));
    }

    public Task<bool> ShowLateReminderAsync()
    {
        return Task.FromResult(ShowReminder(
            _context,
            "It's after 23:00",
            "You still have not completed today's study check-in. Finish it now to keep your streak alive."));
    }

    public Task ScheduleLateReminderAsync(DateTime triggerAt)
    {
        ScheduleLateReminder(_context, triggerAt);
        return Task.CompletedTask;
    }

    public Task CancelScheduledLateReminderAsync()
    {
        CancelScheduledLateReminder(_context);
        return Task.CompletedTask;
    }

    public Task DismissReminderAsync()
    {
        NotificationManagerCompat.From(_context).Cancel(NotificationId);
        return Task.CompletedTask;
    }

    internal static void HandleLateReminderTrigger(Context context)
    {
        var now = DateTime.Now;
        var database = LoadDatabase(context);
        var currentUser = GetCurrentUser(database);
        if (currentUser is null)
        {
            CancelScheduledLateReminder(context);
            return;
        }

        ScheduleLateReminder(context, now.Date.AddDays(1).AddHours(23));

        var reminderDateKey = ReminderPreferenceKeys.GetLateReminderDateKey(currentUser.Id);
        var lastReminderDate = Preferences.Default.Get(reminderDateKey, string.Empty);

        if (HasCheckedInOn(currentUser, now) ||
            string.Equals(lastReminderDate, now.ToString("yyyy-MM-dd"), StringComparison.Ordinal))
        {
            return;
        }

        var shown = ShowReminder(
            context,
            "It's after 23:00",
            "You still have not completed today's study check-in. Finish it now to keep your streak alive.");

        if (!shown)
        {
            return;
        }

        Preferences.Default.Set(reminderDateKey, now.ToString("yyyy-MM-dd"));
    }

    internal static void RescheduleIfLoggedIn(Context context)
    {
        var database = LoadDatabase(context);
        if (GetCurrentUser(database) is null)
        {
            CancelScheduledLateReminder(context);
            return;
        }

        ScheduleLateReminder(context, GetNextReminderTime(DateTime.Now));
    }

    internal static DateTime GetNextReminderTime(DateTime now)
    {
        var todayAtEleven = now.Date.AddHours(23);
        return now < todayAtEleven ? todayAtEleven : todayAtEleven.AddDays(1);
    }

    internal static void ScheduleLateReminder(Context context, DateTime triggerAt)
    {
        var alarmManager = context.GetSystemService(Context.AlarmService) as AlarmManager;
        if (alarmManager is null)
        {
            return;
        }

        var pendingIntent = CreateAlarmPendingIntent(context);
        alarmManager.Cancel(pendingIntent);

        var triggerAtMillis = new DateTimeOffset(triggerAt).ToUnixTimeMilliseconds();
        if (OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            alarmManager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
            return;
        }

        alarmManager.Set(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
    }

    internal static void CancelScheduledLateReminder(Context context)
    {
        var alarmManager = context.GetSystemService(Context.AlarmService) as AlarmManager;
        if (alarmManager is null)
        {
            return;
        }

        alarmManager.Cancel(CreateAlarmPendingIntent(context));
    }

    internal static void EnsureChannel(Context context)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            return;
        }

        var manager = context.GetSystemService(Context.NotificationService) as NotificationManager;
        if (manager?.GetNotificationChannel(ChannelId) is not null)
        {
            return;
        }

        var channel = new NotificationChannel(ChannelId, "Check-in reminders", NotificationImportance.High)
        {
            Description = "Daily study check-in reminders shown in the Android notification tray."
        };

        manager?.CreateNotificationChannel(channel);
    }

    internal static bool ShowReminder(Context context, string title, string message)
    {
        EnsureChannel(context);

        var manager = NotificationManagerCompat.From(context);
        if (!manager.AreNotificationsEnabled())
        {
            return false;
        }

        var launchIntent = new Intent(context, typeof(global::StudyApp.MainActivity));
        launchIntent.SetAction(Intent.ActionMain);
        launchIntent.AddCategory(Intent.CategoryLauncher);
        launchIntent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop | ActivityFlags.NewTask);

        var contentIntent = PendingIntent.GetActivity(
            context,
            NotificationId,
            launchIntent,
            GetPendingIntentFlags());

        var notification = new NotificationCompat.Builder(context, ChannelId)
            .SetSmallIcon(context.ApplicationInfo?.Icon ?? Android.Resource.Drawable.IcDialogInfo)
            .SetContentTitle(title)
            .SetContentText(message)
            .SetStyle(new NotificationCompat.BigTextStyle().BigText(message))
            .SetAutoCancel(true)
            .SetCategory(NotificationCompat.CategoryReminder)
            .SetPriority((int)NotificationCompat.PriorityHigh)
            .SetContentIntent(contentIntent)
            .Build();

        manager.Notify(NotificationId, notification);
        return true;
    }

    private static PendingIntent CreateAlarmPendingIntent(Context context)
    {
        var intent = new Intent(context, typeof(global::StudyApp.LateCheckInReminderReceiver));
        return PendingIntent.GetBroadcast(
            context,
            AlarmRequestCode,
            intent,
            GetPendingIntentFlags())!;
    }

    private static AppDatabase? LoadDatabase(Context context)
    {
        var databasePath = DatabaseConstants.GetSqlitePath(context.FilesDir?.AbsolutePath ?? string.Empty);
        if (!File.Exists(databasePath))
        {
            return null;
        }

        return SqliteAppDatabaseStore.Load(
            databasePath,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)
            {
                WriteIndented = true
            });
    }

    private static UserRecord? GetCurrentUser(AppDatabase? database)
    {
        if (database?.CurrentUserId is null)
        {
            return null;
        }

        return database.Users.FirstOrDefault(user => user.Id == database.CurrentUserId);
    }

    private static bool HasCheckedInOn(UserRecord user, DateTime date)
    {
        var dayKey = date.ToString("yyyy-MM-dd");
        return user.LastCheckInAt?.Date == date.Date || user.CheckInDates.Contains(dayKey);
    }

    private static PendingIntentFlags GetPendingIntentFlags()
    {
        return OperatingSystem.IsAndroidVersionAtLeast(23)
            ? PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent
            : PendingIntentFlags.UpdateCurrent;
    }

    private sealed class NotificationPermission : Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
        [
            ("android.permission.POST_NOTIFICATIONS", true)
        ];
    }
}
#endif
