namespace StudyApp.Models;


/// <summary>
/// Represents an error information model used for UI display.
/// </summary>
public sealed class ErrorModel
{
    public string Header { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string Color { get; set; } = "#7CB8F6";
}
