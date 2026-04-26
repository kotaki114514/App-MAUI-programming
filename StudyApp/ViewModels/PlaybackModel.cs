using StudyApp.Models;

namespace StudyApp.ViewModels;

public sealed class PlaybackModel
{
    public List<string> CameraList { get; } =
    [
        "lec1.png",
        "lec2.png",
        "lec3.png",
        "lec4.png"
    ];

    public List<ErrorModel> ErrorList { get; } =
    [
        new()
        {
            Header = "Assignment missing",
            Description = "Missing submission of Assignment 2.",
            Address = "Math",
            Color = "#D9506E"
        },
        new()
        {
            Header = "Exam ahead",
            Description = "Exam next week.",
            Address = "Java Programming",
            Color = "#F4C95D"
        },
        new()
        {
            Header = "Workshop",
            Description = "Interview workshop on Friday.",
            Address = "Main Hall",
            Color = "#7CB8F6"
        }
    ];
}
