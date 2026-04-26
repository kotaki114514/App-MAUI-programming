using System.Collections.Generic;

namespace StudyApp.Models;


/// <summary>
/// Result returned after a user performs a daily check-in.
/// </summary>
public sealed class CheckInResult
{
    public bool Success { get; init; }

    public bool AlreadyCheckedIn { get; init; }

    public int CurrentStreak { get; init; }

    public string Message { get; init; } = string.Empty;

    public IReadOnlyList<AchievementInfo> NewlyUnlockedAchievements { get; init; } = [];
}
