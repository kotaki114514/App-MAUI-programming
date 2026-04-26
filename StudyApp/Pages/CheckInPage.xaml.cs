using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using StudyApp.Models;
using StudyApp.Services;

namespace StudyApp.Pages;

public partial class CheckInPage : ContentPage
{
    private readonly DailyReminderService _dailyReminderService;
    private readonly UserSessionService _sessionService;

    private bool _canCheckIn = true;
    private string _checkInButtonText = "Check in now";
    private string _checkInHint = "You can check in once per day. Missing a day resets the streak.";
    private bool _isAnimationVisible;
    private double _progressValue;
    private string _progressText = string.Empty;
    private string _streakHeadline = "0 days";
    private string _todayBadgeBackground = "#EAF4FF";
    private string _todayBadgeText = "Pending";
    private string _todayBadgeTextColor = "#345C84";

    public CheckInPage(UserSessionService sessionService, DailyReminderService dailyReminderService)
    {
        InitializeComponent();
        _sessionService = sessionService;
        _dailyReminderService = dailyReminderService;
        BindingContext = this;

        // Refresh UI when user data changes
        _sessionService.DataChanged += async (_, _) => await MainThread.InvokeOnMainThreadAsync(LoadAsync);
    }

    public ObservableCollection<WeekDayItem> WeekDays { get; } = new();


    //    UI Bindings 
    public string StreakHeadline
    {
        get => _streakHeadline;
        set => SetProperty(ref _streakHeadline, value);
    }

    public double ProgressValue
    {
        get => _progressValue;
        set => SetProperty(ref _progressValue, value);
    }

    public string ProgressText
    {
        get => _progressText;
        set => SetProperty(ref _progressText, value);
    }

    public string TodayBadgeText
    {
        get => _todayBadgeText;
        set => SetProperty(ref _todayBadgeText, value);
    }

    public string TodayBadgeBackground
    {
        get => _todayBadgeBackground;
        set => SetProperty(ref _todayBadgeBackground, value);
    }

    public string TodayBadgeTextColor
    {
        get => _todayBadgeTextColor;
        set => SetProperty(ref _todayBadgeTextColor, value);
    }

    public bool CanCheckIn
    {
        get => _canCheckIn;
        set => SetProperty(ref _canCheckIn, value);
    }

    public string CheckInButtonText
    {
        get => _checkInButtonText;
        set => SetProperty(ref _checkInButtonText, value);
    }

    public string CheckInHint
    {
        get => _checkInHint;
        set => SetProperty(ref _checkInHint, value);
    }

    public bool IsAnimationVisible
    {
        get => _isAnimationVisible;
        set => SetProperty(ref _isAnimationVisible, value);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
        await _dailyReminderService.CheckAndNotifyAsync();
    }

    /// <summary>
    /// Load user check-in state and refresh UI.
    /// </summary>
    private async Task LoadAsync()
    {
        var user = await _sessionService.GetCurrentUserAsync();
        if (user is null)
        {
            return;
        }

        var signedToday = user.LastCheckInAt?.Date == DateTime.Today;

        StreakHeadline = $"{user.CurrentStreak} days";
        ProgressValue = Math.Min(user.CurrentStreak, 7) / 7.0;
        ProgressText = $"Weekly progress: {Math.Min(user.CurrentStreak, 7)} / 7 days. Best streak: {user.BestStreak} days.";

        TodayBadgeText = signedToday ? "Done today" : "Pending";
        TodayBadgeBackground = signedToday ? "#E9F8F0" : "#EAF4FF";
        TodayBadgeTextColor = signedToday ? "#216E4A" : "#345C84";

        CanCheckIn = !signedToday;
        CheckInButtonText = signedToday ? "Checked in today" : "Check in now";
        CheckInHint = signedToday
            ? "Today's learning routine is complete. Come back tomorrow to keep the streak alive."
            : "A successful check-in updates your streak and can unlock new achievements.";

        BuildWeekDays(user);
    }

    /// <summary>
    /// Build weekly calendar UI (7 days view).
    /// </summary>
    private void BuildWeekDays(UserRecord user)
    {
        var culture = CultureInfo.GetCultureInfo("en-US");
        var checkedDates = user.CheckInDates.ToHashSet();
        var today = DateTime.Today;
        var monday = today.AddDays(-((7 + (int)today.DayOfWeek - (int)DayOfWeek.Monday) % 7));

        WeekDays.Clear();

        for (var index = 0; index < 7; index++)
        {
            var date = monday.AddDays(index);
            var key = date.ToString("yyyy-MM-dd");
            var checkedIn = checkedDates.Contains(key);
            var isToday = date == today;

            WeekDays.Add(new WeekDayItem
            {
                DayLabel = date.ToString("ddd", culture),
                DayNumber = date.ToString("dd"),
                CardBackground = checkedIn ? "#E6F1FF" : isToday ? "#EEF5FF" : "#FFFFFF",
                BorderColor = checkedIn ? "#7CB8F6" : isToday ? "#B1CEF0" : "#D8E5F3",
                TextColor = checkedIn ? "#345C84" : "#1D2939",
                StatusIcon = checkedIn ? "\u2713" : isToday ? "\u2022" : "\u25CB"
            });
        }
    }

    /// <summary>
    /// Trigger a check-in action, play gif animation, and show notification.
    /// </summary>
    private async void OnCheckInClicked(object? sender, EventArgs e)
    {
        var result = await _sessionService.CheckInAsync();

        if (!result.Success)
        {
            await DisplayAlert("Check-in notice", result.Message, "OK");
            await LoadAsync();
            return;
        }

        await LoadAsync();
        await PlayCheckInAnimationAsync();

        if (result.NewlyUnlockedAchievements.Count > 0)
        {
            var names = string.Join(", ", result.NewlyUnlockedAchievements.Select(item => item.Title));
            await DisplayAlert("Check-in complete", $"Current streak: {result.CurrentStreak} days.\nNew achievements: {names}", "Nice");
        }
        else
        {
            await DisplayAlert("Check-in complete", result.Message, "Keep going");
        }
    }

    /// <summary>
    /// Simple check-in success animation.
    /// </summary>
    private async Task PlayCheckInAnimationAsync()
    {
        IsAnimationVisible = true;
        LikeAnimation.Opacity = 0;
        LikeAnimation.Source = null;
        LikeAnimation.Source = "like.gif";

        await LikeAnimation.FadeTo(1, 250);
        await Task.Delay(1800);
        await LikeAnimation.FadeTo(0, 250);

        IsAnimationVisible = false;
    }


    /// <summary>
    /// Property setter helper .
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
