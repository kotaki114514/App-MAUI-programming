#if ANDROID
using Android.App;
using Android.Content;
using StudyApp.Services;

namespace StudyApp;

[BroadcastReceiver(Enabled = true, Exported = false)]
public sealed class LateCheckInReminderReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null)
        {
            return;
        }

        ReminderNotificationService.HandleLateReminderTrigger(context);
    }
}
#endif
