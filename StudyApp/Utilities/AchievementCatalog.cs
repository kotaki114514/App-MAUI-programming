using System.Collections.Generic;
using StudyApp.Models;

namespace StudyApp.Utilities;

public static class AchievementCatalog
{
    public static IReadOnlyList<AchievementInfo> All { get; } =
    [
        new(1, "First Spark", "Check in for 1 day in a row.", "\U0001F331", "#7BC96F"),
        new(2, "Momentum Start", "Check in for 2 days in a row.", "\U0001F525", "#7CB8F6"),
        new(3, "Steady Rhythm", "Check in for 3 days in a row.", "\u2B50", "#F4C95D"),
        new(4, "Routine Builder", "Check in for 4 days in a row.", "\U0001F4AA", "#5E98D7"),
        new(5, "Learning Heat", "Check in for 5 days in a row.", "\U0001F680", "#7D8EF6"),
        new(6, "Locked In", "Check in for 6 days in a row.", "\U0001F3C5", "#59C0AE"),
        new(7, "Full Week", "Check in for 7 days in a row.", "\U0001F3C6", "#3F84E5")
    ];
}
