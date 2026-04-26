namespace StudyApp.Services;

public sealed class DailyReminderService
{


    // Reminder trigger time (23:00 daily)
    private static readonly TimeSpan LateReminderTime = TimeSpan.FromHours(23);

    private readonly IReminderNotificationService _notificationService;
    private readonly UserSessionService _sessionService;
    private IDispatcherTimer? _timer;
    private bool _isCheckingReminder;

    public DailyReminderService(UserSessionService sessionService, IReminderNotificationService notificationService)
    {
        _sessionService = sessionService;
        _notificationService = notificationService;

        // Sync reminders when session changes
        _sessionService.SessionChanged += async (_, _) => await SyncReminderScheduleAsync();
        _sessionService.DataChanged += async (_, _) => await SyncReminderScheduleAsync();
    }


    /// <summary>
    /// Start background timer for periodic reminder checks
    /// </summary>
    public void Start()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_timer is not null)
            {
                return;
            }

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher is null)
            {
                return;
            }

            _timer = dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromMinutes(1);
            _timer.Tick += async (_, _) => await CheckAndNotifyAsync();
            _timer.Start();
        });

        _ = InitializeAsync();
    }


    /// <summary>
    /// Initialize reminder system and request notification permission
    /// </summary>
    public async Task InitializeAsync(bool requestPermission = false)
    {
        if (requestPermission)
        {
            await _notificationService.RequestPermissionIfNeededAsync();
        }

        await SyncReminderScheduleAsync();
        await CheckAndNotifyAsync();
    }


    /// <summary>
    /// Handle login success and optionally show login reminder
    /// </summary>
    public async Task HandleAuthenticationSuccessAsync(bool showLoginReminder)
    {
        var notificationsEnabled = await _notificationService.RequestPermissionIfNeededAsync();

        await SyncReminderScheduleAsync();

        if (!showLoginReminder || !notificationsEnabled || await _sessionService.HasCheckedInTodayAsync())
        {
            return;
        }

        await _notificationService.ShowLoginReminderAsync();
    }


    /// <summary>
    /// Periodic reminder check (called by timer)
    /// </summary>
    public async Task CheckAndNotifyAsync()
    {
        if (_isCheckingReminder)
        {
            return;
        }

        if (!await _sessionService.ShouldShowReminderAsync(DateTime.Now))
        {
            return;
        }

        _isCheckingReminder = true;

        try
        {


            // Show late-night reminder if needed
            var reminderShown = await _notificationService.ShowLateReminderAsync();
            if (reminderShown)
            {
                await _sessionService.MarkReminderShownAsync(DateTime.Now);
            }
        }
        finally
        {
            _isCheckingReminder = false;
        }
    }


    /// <summary>
    /// Sync reminder schedule based on user state
    /// </summary>
    private async Task SyncReminderScheduleAsync()
    {
        // If not logged in, cancel all reminders
        if (!_sessionService.IsLoggedIn)
        {
            await _notificationService.CancelScheduledLateReminderAsync();
            await _notificationService.DismissReminderAsync();
            return;
        }

        var now = DateTime.Now;
        var checkedToday = await _sessionService.HasCheckedInTodayAsync();


        // Dismiss notification if already checked in
        if (checkedToday)
        {
            await _notificationService.DismissReminderAsync();
        }

        await _notificationService.CancelScheduledLateReminderAsync();

        // Schedule next reminder time
        await _notificationService.ScheduleLateReminderAsync(GetNextReminderTime(now, checkedToday));
    }


    /// <summary>
    /// Calculate next reminder time (23:00 logic)
    /// </summary>
    private static DateTime GetNextReminderTime(DateTime now, bool checkedToday)
    {
        var todayAtLateReminderTime = now.Date.Add(LateReminderTime);

        // If not checked in and before 23:00, remind today
        if (!checkedToday && now < todayAtLateReminderTime)
        {
            return todayAtLateReminderTime;
        }

        // Otherwise schedule for next day
        return todayAtLateReminderTime.AddDays(1);
    }
}
