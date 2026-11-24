// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.PathologyProvider;

/// <summary>
/// Chance of a trauma by the damage of a hit: from <see cref="Threshold"/> it grows linearly, (damage - offset) * per damage, and is clamped.
/// </summary>
public readonly record struct TraumaRoll(float Threshold, float ChanceOffset, float ChancePerDamage, float MinChance, float MaxChance)
{
    public bool TryGetChance(float damage, out float chance)
    {
        chance = 0f;
        if (damage < Threshold)
            return false;

        chance = Math.Clamp((damage - ChanceOffset) * ChancePerDamage, MinChance, MaxChance);
        return true;
    }
}
