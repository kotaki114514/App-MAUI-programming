using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using StudyApp.Services;
using StudyApp.Utilities;
using StudyApp.ViewModels;

namespace StudyApp.Pages;

public partial class HomePage : ContentView
{

    // Shared HttpClient for banner image loading
    private static readonly HttpClient BannerHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private readonly DailyReminderService _dailyReminderService;
    private readonly IServiceProvider _serviceProvider;
    private readonly UserSessionService _sessionService;

    // UI state fields (Home page view state)
    private ImageSource _bannerImageSource = ImageSource.FromFile("dotnet_bot.png");
    private int _bannerRequestVersion;
    private string _bestStreakText = "0";
    private string _currentStreakText = "0";
    private string _goalSummary = "Complete today's check-in first.";
    private string _notesCountText = "0";
    private string _todayStatusColor = "#5E98D7";
    private string _todayStatusText = "You have not checked in today yet.";
    private string _welcomeTitle = "Learner";
    private double _weeklyProgress;

    public HomePage(UserSessionService sessionService, IServiceProvider serviceProvider, DailyReminderService dailyReminderService)
    {
        InitializeComponent();
        _sessionService = sessionService;
        _serviceProvider = serviceProvider;
        _dailyReminderService = dailyReminderService;
        BindingContext = this;


        // Refresh UI when user data changes
        _sessionService.DataChanged += async (_, _) => await MainThread.InvokeOnMainThreadAsync(LoadAsync);
    }


    // Playback + notice data container (shared model)
    public PlaybackModel PlaybackSection { get; } = new();


    //     UI Bindings 
    public ImageSource BannerImageSource
    {
        get => _bannerImageSource;
        set => SetProperty(ref _bannerImageSource, value);
    }

    public string WelcomeTitle
    {
        get => _welcomeTitle;
        set => SetProperty(ref _welcomeTitle, value);
    }

    public string TodayStatusText
    {
        get => _todayStatusText;
        set => SetProperty(ref _todayStatusText, value);
    }

    public string TodayStatusColor
    {
        get => _todayStatusColor;
        set => SetProperty(ref _todayStatusColor, value);
    }

    public string CurrentStreakText
    {
        get => _currentStreakText;
        set => SetProperty(ref _currentStreakText, value);
    }

    public string BestStreakText
    {
        get => _bestStreakText;
        set => SetProperty(ref _bestStreakText, value);
    }

    public string NotesCountText
    {
        get => _notesCountText;
        set => SetProperty(ref _notesCountText, value);
    }

    public string GoalSummary
    {
        get => _goalSummary;
        set => SetProperty(ref _goalSummary, value);
    }

    public double WeeklyProgress
    {
        get => _weeklyProgress;
        set => SetProperty(ref _weeklyProgress, value);
    }


    // Computed UI values
    public int PlaybackCount => PlaybackSection.CameraList.Count;

    public int NoticeCount => PlaybackSection.ErrorList.Count;


    /// <summary>
    /// Refresh full home page data (user + notes + UI state).
    /// </summary>
    public async Task RefreshAsync(bool refreshBanner = false)
    {
        await LoadAsync();

        if (refreshBanner)
        {
            _ = LoadBannerAsync();
        }

        await _dailyReminderService.CheckAndNotifyAsync();
    }



    /// <summary>
    /// Load user-related data and update UI state.
    /// </summary>
    private async Task LoadAsync()
    {
        var user = await _sessionService.GetCurrentUserAsync();
        var notes = await _sessionService.GetCurrentUserNotesAsync();
        if (user is null)
        {
            return;
        }

        var checkedToday = user.LastCheckInAt?.Date == DateTime.Today;

        WelcomeTitle = $"{user.Nickname}, ready to study?";
        TodayStatusText = checkedToday ? "You already checked in today. Keep it going." : "You have not checked in today yet.";
        TodayStatusColor = checkedToday ? "#2E9A68" : "#5E98D7";
        CurrentStreakText = $"{user.CurrentStreak} days";
        BestStreakText = $"{user.BestStreak} days";
        NotesCountText = notes.Count.ToString();

        var progressDays = Math.Min(user.CurrentStreak, 7);
        WeeklyProgress = progressDays / 7.0;
        GoalSummary = checkedToday
            ? $"You completed today's check-in. Only {Math.Max(0, 7 - progressDays)} more day(s) to reach the 7-day milestone."
            : $"Check in today to build a {Math.Max(1, user.CurrentStreak + 1)}-day streak.";
    }



    /// <summary>
    /// Refresh banner image (random online image with fallback).
    /// </summary>
    private void OnRefreshBannerClicked(object? sender, EventArgs e)
    {
        _ = LoadBannerAsync();
    }


    /// <summary>
    /// Navigate to check-in page.
    /// </summary>
    private async void OnCheckInClicked(object? sender, EventArgs e)
    {
        await AppNavigator.PushAsync(_serviceProvider.GetRequiredService<CheckInPage>());
    }



    /// <summary>
    /// Load random banner image safely with version control (avoid race conditions).
    /// </summary>
    private async Task LoadBannerAsync()
    {
        var requestVersion = Interlocked.Increment(ref _bannerRequestVersion);


        // reset to default image first
        BannerImageSource = ImageSource.FromFile("dotnet_bot.png");

        try
        {
            var url = $"https://www.dmoe.cc/random.php?seed={DateTimeOffset.Now.ToUnixTimeMilliseconds()}";
            var bytes = await BannerHttpClient.GetByteArrayAsync(url);


            // ignore outdated requests
            if (requestVersion != _bannerRequestVersion || bytes.Length == 0)
            {
                return;
            }

            BannerImageSource = ImageSource.FromStream(() => new MemoryStream(bytes));
        }
        catch
        {

            // fallback image
            if (requestVersion == _bannerRequestVersion)
            {
                BannerImageSource = ImageSource.FromFile("dotnet_bot.png");
            }
        }
    }


    /// <summary>
    /// Property helper (INotifyPropertyChanged).
    /// </summary>
    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
