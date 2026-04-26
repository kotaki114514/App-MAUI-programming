namespace StudyApp.Models;

/// <summary>
/// Represents a single day item in the week view UI.
/// </summary>
public sealed class WeekDayItem
{
    public string DayLabel { get; set; } = string.Empty;

    public string DayNumber { get; set; } = string.Empty;

    public string CardBackground { get; set; } = "#FFFFFF";

    public string BorderColor { get; set; } = "#D5DFEA";

    public string TextColor { get; set; } = "#1D2939";

    public string StatusIcon { get; set; } = "\u25CB";
}
