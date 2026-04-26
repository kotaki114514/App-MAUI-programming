namespace StudyApp.Models;

// Represents a fixed achievement definition
public sealed record AchievementInfo(
    int RequiredDays,
    string Title,
    string Description,
    string Icon,
    string AccentColor);
