using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using StudyApp.Models;
using StudyApp.Services;
using StudyApp.Utilities;

namespace StudyApp.Pages;

public partial class ProfilePage : ContentView
{
    private const int MaxAvatarSizeBytes = 2 * 1024 * 1024;

    private readonly UserSessionService _sessionService;

    private string _avatarColor = "#7CB8F6";
    private string _avatarEmoji = "\U0001F4DA";
    private string _avatarHintText = "JPG, PNG, and WEBP are supported. Square images work best.";
    private ImageSource? _avatarImageSource;
    private string _bestStreakText = "0 days";
    private bool _clearCustomAvatar;
    private string _currentStreakText = "0 days";
    private string _databaseHint = string.Empty;
    private string _displayName = string.Empty;
    private bool _hasCustomAvatar;
    private string _nicknameInput = string.Empty;
    private string _notesCountText = "0";
    private string? _pendingAvatarBase64;
    private AvatarOption? _selectedAvatar = AvatarCatalog.All[0];
    private string _userNameText = string.Empty;

    public ProfilePage(UserSessionService sessionService)
    {
        InitializeComponent();
        _sessionService = sessionService;

        // Bind page to itself (MVVM-style)
        BindingContext = this;


        // Refresh UI when user data changes
        _sessionService.DataChanged += async (_, _) => await MainThread.InvokeOnMainThreadAsync(LoadAsync);
    }


    // Available default avatar options
    public ObservableCollection<AvatarOption> AvatarOptions { get; } = new(AvatarCatalog.All);


    // Achievement cards displayed in UI
    public ObservableCollection<AchievementCardItem> Achievements { get; } = new();


    // ===== Properties (data binding) =====
    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    public string NicknameInput
    {
        get => _nicknameInput;
        set => SetProperty(ref _nicknameInput, value);
    }

    public string UserNameText
    {
        get => _userNameText;
        set => SetProperty(ref _userNameText, value);
    }

    public string DatabaseHint
    {
        get => _databaseHint;
        set => SetProperty(ref _databaseHint, value);
    }

    public string AvatarEmoji
    {
        get => _avatarEmoji;
        set => SetProperty(ref _avatarEmoji, value);
    }

    public string AvatarColor
    {
        get => _avatarColor;
        set => SetProperty(ref _avatarColor, value);
    }

    public ImageSource? AvatarImageSource
    {
        get => _avatarImageSource;
        set => SetProperty(ref _avatarImageSource, value);
    }

    public bool HasCustomAvatar
    {
        get => _hasCustomAvatar;
        set
        {
            if (SetProperty(ref _hasCustomAvatar, value))
            {
                OnPropertyChanged(nameof(ShowEmojiAvatar));
                OnPropertyChanged(nameof(CanResetCustomAvatar));
            }
        }
    }

    public bool ShowEmojiAvatar => !HasCustomAvatar;

    public bool CanResetCustomAvatar => HasCustomAvatar;

    public string AvatarHintText
    {
        get => _avatarHintText;
        set => SetProperty(ref _avatarHintText, value);
    }


    // Selected default avatar
    public AvatarOption? SelectedAvatar
    {
        get => _selectedAvatar;
        set
        {
            if (value is not null && SetProperty(ref _selectedAvatar, value))
            {
                AvatarEmoji = value.Emoji;
                AvatarColor = value.ColorHex;
                UpdateAvatarHintText();
            }
        }
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

    public Task RefreshAsync()
    {
        return LoadAsync();
    }


    /// <summary>
    /// Load user profile, notes, and achievements
    /// </summary>
    private async Task LoadAsync()
    {
        var user = await _sessionService.GetCurrentUserAsync();
        var notes = await _sessionService.GetCurrentUserNotesAsync();
        var achievements = await _sessionService.GetAchievementCardsAsync();

        if (user is null)
        {
            return;
        }

        // Reset pending avatar state
        _pendingAvatarBase64 = null;
        _clearCustomAvatar = false;

        DisplayName = user.Nickname;
        NicknameInput = user.Nickname;
        UserNameText = $"Username: {user.UserName}";
        DatabaseHint = $"Local database: {Path.GetFileName(_sessionService.DatabasePath)}";

        SelectedAvatar = AvatarCatalog.GetByEmoji(user.AvatarEmoji);
        ApplyAvatarPreview(user.AvatarImageBase64);
        CurrentStreakText = $"{user.CurrentStreak} days";
        BestStreakText = $"{user.BestStreak} days";
        NotesCountText = notes.Count.ToString();

        // Load achievements
        Achievements.Clear();
        foreach (var achievement in achievements)
        {
            Achievements.Add(achievement);
        }
    }


    /// <summary>
    /// Upload avatar from local file
    /// </summary>
    private async void OnUploadAvatarClicked(object? sender, EventArgs e)
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Choose an avatar image",
                FileTypes = FilePickerFileType.Images
            });

            if (file is null)
            {
                return;
            }

            await using var stream = await file.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);

            var bytes = memoryStream.ToArray();
            if (bytes.Length == 0)
            {
                await AppNavigator.DisplayAlertAsync("Upload failed", "The image could not be read. Try another file.", "OK");
                return;
            }

            if (bytes.Length > MaxAvatarSizeBytes)
            {
                await AppNavigator.DisplayAlertAsync("Image too large", "Please choose an image smaller than 2MB.", "OK");
                return;
            }

            _pendingAvatarBase64 = Convert.ToBase64String(bytes);
            _clearCustomAvatar = false;
            AvatarImageSource = CreateImageSource(bytes);


            // Preview image
            HasCustomAvatar = true;
            AvatarHintText = $"Selected image: {file.FileName}. Tap Save profile to apply it.";
        }
        catch
        {
            await AppNavigator.DisplayAlertAsync("Upload failed", "This image could not be opened. Try another file.", "OK");
        }
    }


    /// <summary>
    /// Reset avatar to default
    /// </summary>
    private void OnResetAvatarClicked(object? sender, EventArgs e)
    {
        _pendingAvatarBase64 = null;
        _clearCustomAvatar = true;
        AvatarImageSource = null;
        HasCustomAvatar = false;
        UpdateAvatarHintText();
    }



    /// <summary>
    /// Save updated profile
    /// </summary>
    private async void OnSaveProfileClicked(object? sender, EventArgs e)
    {
        var result = await _sessionService.UpdateProfileAsync(
            NicknameInput,
            SelectedAvatar?.Emoji ?? AvatarCatalog.All[0].Emoji,
            _pendingAvatarBase64,
            _clearCustomAvatar);

        if (!result.Success)
        {
            await AppNavigator.DisplayAlertAsync("Save failed", result.Message, "OK");
            return;
        }

        NicknameInput = NicknameInput.Trim();
        DisplayName = NicknameInput;
        _pendingAvatarBase64 = null;
        _clearCustomAvatar = false;
        await AppNavigator.DisplayAlertAsync("Saved", result.Message, "OK");
    }


    /// <summary>
    /// Logout current user
    /// </summary>
    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        var confirm = await AppNavigator.DisplayAlertAsync("Sign out", "Do you want to sign out of this local account?", "Sign out", "Cancel");
        if (!confirm)
        {
            return;
        }

        await _sessionService.LogoutAsync();
    }


    /// <summary>
    /// Show local database path
    /// </summary>
    private async void OnDatabaseHintTapped(object? sender, TappedEventArgs e)
    {
        await AppNavigator.DisplayAlertAsync(
            "Local database path",
            _sessionService.DatabasePath,
            "OK");
    }


    /// <summary>
    /// Apply avatar preview from Base64
    /// </summary>
    private void ApplyAvatarPreview(string? avatarBase64)
    {
        if (string.IsNullOrWhiteSpace(avatarBase64))
        {
            AvatarImageSource = null;
            HasCustomAvatar = false;
            UpdateAvatarHintText();
            return;
        }

        try
        {
            var bytes = Convert.FromBase64String(avatarBase64);
            AvatarImageSource = CreateImageSource(bytes);
            HasCustomAvatar = true;
        }
        catch
        {
            AvatarImageSource = null;
            HasCustomAvatar = false;
        }

        UpdateAvatarHintText();
    }


    /// <summary>
    /// Update hint text based on avatar state
    /// </summary>
    private void UpdateAvatarHintText()
    {
        AvatarHintText = HasCustomAvatar
            ? "A custom uploaded avatar is active. Use Reset to default to switch back."
            : $"Current default avatar: {SelectedAvatar?.Label ?? "Learning Book"}. JPG, PNG, and WEBP are supported.";
    }

    private static ImageSource CreateImageSource(byte[] bytes)
    {
        return ImageSource.FromStream(() => new MemoryStream(bytes));
    }

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
