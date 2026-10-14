// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Utility;

namespace Content.Client.SS220.Silicons.StationAi.UI;

/// <summary>
/// Health label and dot colour, matching the crew monitoring console (SS220-colorful-sensors).
/// </summary>
public static class StationAiCrewHealth
{
    private static readonly ResPath Rsi = new("Interface/Alerts/human_crew_monitoring.rsi");

    public static (SpriteSpecifier Icon, Color Dot) Get(bool alive, float? damage)
    {
        if (!alive)
            return (new SpriteSpecifier.Rsi(Rsi, "dead"), Color.Violet);

        if (damage is not { } value)
            return (new SpriteSpecifier.Rsi(Rsi, "alive"), Color.LightGray);

        if (value >= 1f)
            return (new SpriteSpecifier.Rsi(Rsi, "critical"), Color.Red);

        var index = (int) MathF.Round(4f * value);
        var dot = index switch
        {
            0 or 1 => Color.LimeGreen,
            2 => Color.Gold,
            3 or 4 => Color.Crimson,
            _ => Color.LightGray,
        };
        return (new SpriteSpecifier.Rsi(Rsi, "health" + index), dot);
    }

    /// <summary>Changes only when the shown label changes, so small damage ticks don't rebuild the list.</summary>
    public static int Bucket(bool alive, float? damage)
    {
        if (!alive)
            return -2;
        if (damage is not { } value)
            return -1;
        return value >= 1f ? 5 : (int) MathF.Round(4f * value);
    }
}
