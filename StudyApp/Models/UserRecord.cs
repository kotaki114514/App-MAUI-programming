using System;
using System.Collections.Generic;

namespace StudyApp.Models;

/// <summary>
/// Represents a user in the system. (MY Core user model)
/// </summary>
public sealed class UserRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Nickname { get; set; } = string.Empty;

    public string AvatarEmoji { get; set; } = "\U0001F4DA";

    public string AvatarColorHex { get; set; } = "#7CB8F6";

    public string? AvatarImageBase64 { get; set; }

    public DateTime RegisteredAt { get; set; } = DateTime.Now;

    public List<string> CheckInDates { get; set; } = [];

    public int CurrentStreak { get; set; }

    public int BestStreak { get; set; }

    public DateTime? LastCheckInAt { get; set; }

    public List<int> UnlockedAchievementDays { get; set; } = [];

    public DateTime? LastReminderAt { get; set; }
}
