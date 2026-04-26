using Microsoft.Extensions.Logging;
using StudyApp.Pages;
using StudyApp.Services;

namespace StudyApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<UserSessionService>();
        builder.Services.AddSingleton<DailyReminderService>();
#if ANDROID
        builder.Services.AddSingleton<IReminderNotificationService, ReminderNotificationService>();
#else
        builder.Services.AddSingleton<IReminderNotificationService, NoOpReminderNotificationService>();
#endif

        builder.Services.AddTransient<AppShell>();
        builder.Services.AddTransient<AuthPage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<CheckInPage>();
        builder.Services.AddTransient<NotesPage>();
        builder.Services.AddTransient<NewNotePage>();
        builder.Services.AddTransient<ProfilePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
