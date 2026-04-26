using System.Collections.Generic;
using System.Linq;
using StudyApp.Models;

namespace StudyApp.Utilities;

public static class AvatarCatalog
{
    public static IReadOnlyList<AvatarOption> All { get; } =
    [
        new("\U0001F4DA", "Learning Book", "#7CB8F6"),
        new("\U0001F9E0", "Focus Mind", "#9FD3FF"),
        new("\U0001F3AF", "Target Plan", "#59C0AE"),
        new("\U0001F319", "Night Reader", "#7D8EF6"),
        new("\u2615", "Coffee Partner", "#8DA6BF"),
        new("\U0001F3C6", "Goal Winner", "#D9506E")
    ];

    public static AvatarOption GetByEmoji(string? emoji)
    {
        return All.FirstOrDefault(option => option.Emoji == emoji) ?? All[0];
    }
}
