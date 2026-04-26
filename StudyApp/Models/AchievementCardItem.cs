namespace StudyApp.Models;

// Represents an achievement card displayed in the UI
public sealed class AchievementCardItem
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public string AccentColor { get; set; } = "#7CB8F6";

    public string StatusText { get; set; } = string.Empty;

    public string CardBackground { get; set; } = "#FFFFFF";

    public double CardOpacity { get; set; } = 1.0;
}
