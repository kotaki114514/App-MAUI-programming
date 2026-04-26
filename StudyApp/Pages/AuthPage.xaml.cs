using System.Runtime.CompilerServices;
using StudyApp.Services;

namespace StudyApp.Pages;

public partial class AuthPage : ContentPage
{
    private readonly DailyReminderService _dailyReminderService;
    private readonly UserSessionService _sessionService;


    // Input fields (bound to UI)
    private string _confirmPassword = string.Empty;
    private bool _isRegisterMode;
    private string _nickname = string.Empty;
    private string _password = string.Empty;
    private string _userName = string.Empty;

    public AuthPage(UserSessionService sessionService, DailyReminderService dailyReminderService)
    {
        InitializeComponent();
        _sessionService = sessionService;
        _dailyReminderService = dailyReminderService;

        // Use this page itself as BindingContext
        BindingContext = this;
        RefreshModeProperties();
    }


    // Username input
    public string UserName
    {
        get => _userName;
        set => SetProperty(ref _userName, value);
    }


    // Password input
    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }


    // Confirm password (register only)
    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => SetProperty(ref _confirmPassword, value);
    }


    // Display name (register only)
    public string Nickname
    {
        get => _nickname;
        set => SetProperty(ref _nickname, value);
    }


    // Toggle between login and register mode
    public bool IsRegisterMode
    {
        get => _isRegisterMode;
        set
        {
            if (SetProperty(ref _isRegisterMode, value))
            {
                RefreshModeProperties();
            }
        }
    }


    // UI texts (based on current mode)
    public string ModeTitle => IsRegisterMode ? "Create a local account" : "Welcome back";

    public string ModeDescription => IsRegisterMode
        ? "Register once and keep all of your study data on this device."
        : "Use your existing local account to continue learning.";

    public string SubmitButtonText => IsRegisterMode ? "Register and continue" : "Sign in";

    public string SwitchPrompt => IsRegisterMode ? "Already have an account?" : "Need an account?";

    public string SwitchButtonText => IsRegisterMode ? "Sign in" : "Register";


    // Button styling (login/register toggle UI)
    public string LoginModeBackground => IsRegisterMode ? "#E6F1FF" : "#7CB8F6";

    public string RegisterModeBackground => IsRegisterMode ? "#7CB8F6" : "#E6F1FF";

    public string LoginModeTextColor => IsRegisterMode ? "#345C84" : "#FFFFFF";

    public string RegisterModeTextColor => IsRegisterMode ? "#FFFFFF" : "#345C84";



    // Submit login/register action
    private async void OnSubmitClicked(object? sender, EventArgs e)
    {
        var isRegistering = IsRegisterMode;
        (bool Success, string Message) result;

        if (isRegistering)
        {
            // Validate password match
            if (Password != ConfirmPassword)
            {

                await DisplayAlert("Registration failed", "The two passwords do not match.", "OK");
                return;
            }

            result = await _sessionService.RegisterAsync(UserName, Password, Nickname);
        }
        else
        {
            result = await _sessionService.LoginAsync(UserName, Password);
        }

        if (!result.Success)
        {
            await DisplayAlert(IsRegisterMode ? "Registration failed" : "Sign-in failed", result.Message, "OK");
            return;
        }


        // Trigger post-login reminder logic
        await _dailyReminderService.HandleAuthenticationSuccessAsync(showLoginReminder: !isRegistering);

        // Clear sensitive fields
        Password = string.Empty;
        ConfirmPassword = string.Empty;
    }


    // Switch between login/register modes
    private void OnSwitchModeClicked(object? sender, EventArgs e)
    {
        IsRegisterMode = !IsRegisterMode;
    }

    // Force login mode
    private void OnLoginModeClicked(object? sender, EventArgs e)
    {
        IsRegisterMode = false;
    }

    // Force register mode
    private void OnRegisterModeClicked(object? sender, EventArgs e)
    {
        IsRegisterMode = true;
    }

    // Refresh UI-related computed properties
    private void RefreshModeProperties()
    {
        OnPropertyChanged(nameof(IsRegisterMode));
        OnPropertyChanged(nameof(ModeTitle));
        OnPropertyChanged(nameof(ModeDescription));
        OnPropertyChanged(nameof(SubmitButtonText));
        OnPropertyChanged(nameof(SwitchPrompt));
        OnPropertyChanged(nameof(SwitchButtonText));
        OnPropertyChanged(nameof(LoginModeBackground));
        OnPropertyChanged(nameof(RegisterModeBackground));
        OnPropertyChanged(nameof(LoginModeTextColor));
        OnPropertyChanged(nameof(RegisterModeTextColor));
    }


    // Generic property setter helper (INotifyPropertyChanged style)
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
