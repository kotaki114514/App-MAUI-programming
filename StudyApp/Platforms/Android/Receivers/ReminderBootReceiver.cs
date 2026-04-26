#if ANDROID
using Android.App;
using Android.Content;
using StudyApp.Services;

namespace StudyApp;

[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter(new[] { Intent.ActionBootCompleted, Intent.ActionMyPackageReplaced })]
public sealed class ReminderBootReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null)
        {
            return;
        }

        ReminderNotificationService.RescheduleIfLoggedIn(context);
    }
}
#endif
