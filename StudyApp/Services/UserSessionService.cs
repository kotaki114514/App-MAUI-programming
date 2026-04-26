using StudyApp.Models;
using StudyApp.Utilities;

namespace StudyApp.Services;

/// <summary>
/// Manages the current user session and all user-related business logic,
/// including authentication, profile, notes, check-ins, and reminders.
/// </summary>
public sealed class UserSessionService
{
    private readonly DatabaseService _databaseService;
    private bool _initialized;

    public UserSessionService(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public event EventHandler? SessionChanged;

    public event EventHandler? DataChanged;

    public string? CurrentUserId { get; private set; }

    public bool IsLoggedIn => !string.IsNullOrWhiteSpace(CurrentUserId);

    public string DatabasePath => _databaseService.DatabasePath;

    /// <summary>
    /// Initializes session state and restores last logged-in user if exists.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        await _databaseService.InitializeAsync();
        CurrentUserId = await _databaseService.ReadAsync(database => database.CurrentUserId);
        _initialized = true;
    }

    /// <summary>
    /// Returns a snapshot of the current user to avoid direct database coupling.
    /// </summary>
    public async Task<UserRecord?> GetCurrentUserAsync()
    {
        await InitializeAsync();

        if (!IsLoggedIn)
        {
            return null;
        }

        return await _databaseService.ReadAsync(database =>
        {
            var user = database.Users.FirstOrDefault(item => item.Id == CurrentUserId);
            return CloneHelper.Clone(user);
        });
    }

    /// <summary>
    /// Gets notes for the current user sorted by creation time (descending).
    /// </summary>
    public async Task<IReadOnlyList<NoteRecord>> GetCurrentUserNotesAsync()
    {
        await InitializeAsync();

        if (!IsLoggedIn)
        {
            return [];
        }

        return await _databaseService.ReadAsync(database =>
        {
            var notes = database.Notes
                .Where(note => note.UserId == CurrentUserId)
                .OrderByDescending(note => note.CreatedAt)
                .Select(note => CloneHelper.Clone(note)!)
                .ToList();

            return (IReadOnlyList<NoteRecord>)notes;
        });
    }

    /// <summary>
    /// Registers a new local user and automatically logs them in.
    /// </summary>
    public async Task<(bool Success, string Message)> RegisterAsync(string userName, string password, string nickname)
    {
        await InitializeAsync();

        userName = userName.Trim();
        password = password.Trim();
        nickname = nickname.Trim();

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            return (false, "Please enter a username and password.");
        }

        if (password.Length < 4)
        {
            return (false, "The password must be at least 4 characters long.");
        }

        var avatar = AvatarCatalog.All[0];

        var result = await _databaseService.WriteAsync(database =>
        {
            if (database.Users.Any(item => string.Equals(item.UserName, userName, StringComparison.OrdinalIgnoreCase)))
            {
                return (false, "This username already exists. Please sign in instead.");
            }

            var user = new UserRecord
            {
                UserName = userName,
                Password = password,
                Nickname = string.IsNullOrWhiteSpace(nickname) ? userName : nickname,
                AvatarEmoji = avatar.Emoji,
                AvatarColorHex = avatar.ColorHex,
                RegisteredAt = DateTime.Now
            };

            database.Users.Add(user);
            database.CurrentUserId = user.Id;
            return (true, "Registration complete. You are now signed in.");
        });

        if (result.Item1)
        {
            CurrentUserId = await _databaseService.ReadAsync(database => database.CurrentUserId);
            
            RaiseSessionChanged();
            RaiseDataChanged();
        }

        return result;
    }

    /// <summary>
    /// Validates credentials and sets current user session.
    /// </summary>
    public async Task<(bool Success, string Message)> LoginAsync(string userName, string password)
    {
        await InitializeAsync();

        userName = userName.Trim();
        password = password.Trim();

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            return (false, "Please enter a username and password.");
        }

        var result = await _databaseService.WriteAsync(database =>
        {
            var user = database.Users.FirstOrDefault(item =>
                string.Equals(item.UserName, userName, StringComparison.OrdinalIgnoreCase) &&
                item.Password == password);

            if (user is null)
            {
                return (false, "The username or password is incorrect.");
            }

            database.CurrentUserId = user.Id;
            return (true, $"Welcome back, {user.Nickname}.");
        });

        if (result.Item1)
        {
            CurrentUserId = await _databaseService.ReadAsync(database => database.CurrentUserId);
           
            RaiseSessionChanged();
            RaiseDataChanged();
        }

        return result;
    }

    /// <summary>
    /// Clears current session (logout).
    /// </summary>
    public async Task LogoutAsync()
    {
        await InitializeAsync();

        await _databaseService.WriteAsync(database =>
        {
            database.CurrentUserId = null;
            return true;
        });

        CurrentUserId = null;
        RaiseSessionChanged();
        RaiseDataChanged();
    }

    /// <summary>
    /// Updates user profile including nickname and avatar.
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateProfileAsync(
        string nickname,
        string avatarEmoji,
        string? avatarImageBase64 = null,
        bool clearCustomAvatar = false)
    {
        await InitializeAsync();

        if (!IsLoggedIn)
        {
            return (false, "Please sign in first.");
        }

        nickname = nickname.Trim();
        if (string.IsNullOrWhiteSpace(nickname))
        {
            return (false, "Nickname cannot be empty.");
        }

        var avatar = AvatarCatalog.GetByEmoji(avatarEmoji);

        var result = await _databaseService.WriteAsync(database =>
        {
            var user = database.Users.FirstOrDefault(item => item.Id == CurrentUserId);
            if (user is null)
            {
                return (false, "The current user could not be found.");
            }

            user.Nickname = nickname;
            user.AvatarEmoji = avatar.Emoji;
            user.AvatarColorHex = avatar.ColorHex;

            if (clearCustomAvatar)
            {
                user.AvatarImageBase64 = null;
            }
            else if (!string.IsNullOrWhiteSpace(avatarImageBase64))
            {
                user.AvatarImageBase64 = avatarImageBase64;
            }

            return (true, "Your profile has been saved.");
        });

        if (result.Item1)
        {
            RaiseDataChanged();
        }

        return result;
    }

    /// <summary>
    /// Adds a new note for current user.
    /// </summary>
    public async Task<(bool Success, string Message)> AddNoteAsync(string title, string content)
    {
        await InitializeAsync();

        if (!IsLoggedIn)
        {
            return (false, "Please sign in first.");
        }

        title = title.Trim();
        content = content.Trim();

        if (string.IsNullOrWhiteSpace(title))
        {
            return (false, "Please enter a note title.");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return (false, "Please enter note content.");
        }

        await _databaseService.WriteAsync(database =>
        {
            database.Notes.Add(new NoteRecord
            {
                UserId = CurrentUserId!,
                Title = title,
                Content = content,
                CreatedAt = DateTime.Now
            });

            return true;
        });

        RaiseDataChanged();
        return (true, "Your note has been published.");
    }

    /// <summary>
    /// Updates an existing note belonging to current user.
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateNoteAsync(string noteId, string title, string content)
    {
        await InitializeAsync();

        if (!IsLoggedIn)
        {
            return (false, "Please sign in first.");
        }

        noteId = noteId.Trim();
        title = title.Trim();
        content = content.Trim();

        if (string.IsNullOrWhiteSpace(noteId))
        {
            return (false, "This note no longer exists.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return (false, "Please enter a note title.");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return (false, "Please enter note content.");
        }

        var result = await _databaseService.WriteAsync(database =>
        {
            var note = database.Notes.FirstOrDefault(item => item.Id == noteId && item.UserId == CurrentUserId);
            if (note is null)
            {
                return (false, "This note does not exist or has already been deleted.");
            }

            note.Title = title;
            note.Content = content;
            return (true, "Your note has been saved.");
        });

        if (result.Item1)
        {
            RaiseDataChanged();
        }

        return result;
    }

    /// <summary>
    /// Deletes a note owned by the current user.
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteNoteAsync(string noteId)
    {
        await InitializeAsync();

        if (!IsLoggedIn)
        {
            return (false, "Please sign in first.");
        }

        noteId = noteId.Trim();
        if (string.IsNullOrWhiteSpace(noteId))
        {
            return (false, "This note no longer exists.");
        }

        var result = await _databaseService.WriteAsync(database =>
        {
            var note = database.Notes.FirstOrDefault(item => item.Id == noteId && item.UserId == CurrentUserId);
            if (note is null)
            {
                return (false, "This note does not exist or has already been deleted.");
            }

            database.Notes.Remove(note);
            return (true, "Your note has been deleted.");
        });

        if (result.Item1)
        {
            RaiseDataChanged();
        }

        return result;
    }

    /// <summary>
    /// Executes daily check-in and updates streak + achievements.
    /// </summary>
    public async Task<CheckInResult> CheckInAsync()
    {
        await InitializeAsync();

        if (!IsLoggedIn)
        {
            return new CheckInResult
            {
                Success = false,
                Message = "Please sign in first."
            };
        }

        var result = await _databaseService.WriteAsync(database =>
        {
            var user = database.Users.FirstOrDefault(item => item.Id == CurrentUserId);
            if (user is null)
            {
                return new CheckInResult
                {
                    Success = false,
                    Message = "The current user could not be found."
                };
            }

            var today = DateTime.Today;
            var todayKey = today.ToString("yyyy-MM-dd");

            // Prevent duplicate check-ins per day
            if (user.LastCheckInAt?.Date == today || user.CheckInDates.Contains(todayKey))
            {
                return new CheckInResult
                {
                    Success = false,
                    AlreadyCheckedIn = true,
                    CurrentStreak = user.CurrentStreak,
                    Message = "You already checked in today. Come back tomorrow."
                };
            }

            // Update streak logic
            if (user.LastCheckInAt?.Date == today.AddDays(-1))
            {
                user.CurrentStreak += 1;
            }
            else
            {
                user.CurrentStreak = 1;
            }

            user.LastCheckInAt = DateTime.Now;
            user.CheckInDates.Add(todayKey);
            user.BestStreak = Math.Max(user.BestStreak, user.CurrentStreak);

            // Only shows new and unlocked achivements.
            var newAchievements = AchievementCatalog.All
                .Where(item => user.CurrentStreak >= item.RequiredDays && !user.UnlockedAchievementDays.Contains(item.RequiredDays))
                .ToList();

            foreach (var achievement in newAchievements)
            {
                user.UnlockedAchievementDays.Add(achievement.RequiredDays);
            }

            return new CheckInResult
            {
                Success = true,
                CurrentStreak = user.CurrentStreak,
                Message = "Check-in complete. Nice work today.",
                NewlyUnlockedAchievements = newAchievements
            };
        });

        if (result.Success)
        {
            RaiseDataChanged();
        }

        return result;
    }

    /// <summary>
    /// Checks if user has checked in today.
    /// </summary>
    public async Task<bool> HasCheckedInTodayAsync()
    {
        var user = await GetCurrentUserAsync();
        return user is not null && HasCheckedInOn(user, DateTime.Today);
    }

    /// <summary>
    /// Determines whether a late-day reminder should be shown.
    /// </summary>
    public async Task<bool> ShouldShowReminderAsync(DateTime now)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return false;
        }

        if (now.TimeOfDay < TimeSpan.FromHours(23))
        {
            return false;
        }

        if (HasCheckedInOn(user, now))
        {
            return false;
        }

        var lastReminderDate = Preferences.Default.Get(ReminderPreferenceKeys.GetLateReminderDateKey(user.Id), string.Empty);
        return !string.Equals(lastReminderDate, now.ToString("yyyy-MM-dd"), StringComparison.Ordinal);
    }

    /// <summary>
    /// Marks reminder as shown for today.
    /// </summary>
    public Task MarkReminderShownAsync(DateTime now)
    {
        if (!IsLoggedIn)
        {
            return Task.CompletedTask;
        }

        Preferences.Default.Set(
            ReminderPreferenceKeys.GetLateReminderDateKey(CurrentUserId!),
            now.ToString("yyyy-MM-dd"));

        RaiseDataChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Builds achievement cards for UI display.
    /// </summary>
    public async Task<IReadOnlyList<AchievementCardItem>> GetAchievementCardsAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return [];
        }

        var cards = AchievementCatalog.All
            .Select(item =>
            {
                var unlocked = user.UnlockedAchievementDays.Contains(item.RequiredDays);
                return new AchievementCardItem
                {
                    Title = item.Title,
                    Description = item.Description,
                    Icon = item.Icon,
                    AccentColor = item.AccentColor,
                    StatusText = unlocked ? "Unlocked" : "Locked",
                    CardBackground = unlocked ? "#F6FAFF" : "#F1F5FA",
                    CardOpacity = unlocked ? 1.0 : 0.65
                };
            })
            .ToList();

        return cards;
    }

    private void RaiseSessionChanged()
    {
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RaiseDataChanged()
    {
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    private static bool HasCheckedInOn(UserRecord user, DateTime date)
    {
        var dayKey = date.ToString("yyyy-MM-dd");
        return user.LastCheckInAt?.Date == date.Date || user.CheckInDates.Contains(dayKey);
    }
}
