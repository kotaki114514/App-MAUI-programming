using Microsoft.Extensions.DependencyInjection;
using StudyApp.Pages;
using StudyApp.Services;

namespace StudyApp;

public partial class App : Application
{
    private readonly DailyReminderService _dailyReminderService;
    private readonly IServiceProvider _serviceProvider;
    private readonly UserSessionService _sessionService;

    public App(IServiceProvider serviceProvider, UserSessionService sessionService, DailyReminderService dailyReminderService)
    {
        InitializeComponent();

        _serviceProvider = serviceProvider;
        _sessionService = sessionService;
        _dailyReminderService = dailyReminderService;

        MainPage = CreateLoadingPage();
        _sessionService.SessionChanged += OnSessionChanged;

        _ = InitializeAsync();
    }

    /// <summary>
    /// Restores session state on startup and sets the correct root page.
    /// </summary>
    private async Task InitializeAsync()
    {
        await _sessionService.InitializeAsync();

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            SetRootPage();
            _dailyReminderService.Start();
        });
    }
    /// <summary>
    /// Re-evaluates root page when session state changes (login/logout).
    /// </summary>
    private void OnSessionChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(SetRootPage);
    }

    /// <summary>
    /// Switches between authenticated shell and authentication page.
    /// </summary>
    private void SetRootPage()
    {
        MainPage = _sessionService.IsLoggedIn
            ? new NavigationPage(_serviceProvider.GetRequiredService<AppShell>())
            : new NavigationPage(_serviceProvider.GetRequiredService<AuthPage>());
    }


    /// <summary>
    /// Creates a minimal loading UI shown during startup initialization.
    /// </summary>
    private static Page CreateLoadingPage()
    {
        return new ContentPage
        {
            BackgroundColor = Color.FromArgb("#F3F8FF"),
            Content = new Grid
            {
                Children =
                {
                    new ActivityIndicator
                    {
                        IsRunning = true,
                        Color = Color.FromArgb("#7CB8F6"),
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    }
                }
            }
        };
    }
}
