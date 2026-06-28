using System;

namespace SysBot.Pokemon.Discord.Models;

public static class XpProgression
{
    public static int GetRequiredXPForNextLevel(int currentLevel)
    {
        var level = Math.Max(1, currentLevel);
        var required = 100 + (level * 12) + (int)Math.Round(Math.Pow(level, 1.25) * 6);
        return Math.Max(100, RoundToNearest(required, 5));
    }

    private static int RoundToNearest(int value, int step) =>
        (int)Math.Round(value / (double)step) * step;
}
