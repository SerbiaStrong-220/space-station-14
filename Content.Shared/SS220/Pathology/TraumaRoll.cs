// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Pathology;

/// <summary>
/// Chance of a trauma (or of tearing a body part off) by the damage of a hit: from <see cref="Threshold"/> it grows linearly, (damage - offset) * per damage, and is clamped.
/// </summary>
[DataDefinition]
public partial record struct TraumaRoll()
{
    /// <summary>
    /// Damage of one hit from which the trauma can happen.
    /// </summary>
    [DataField(required: true)]
    public float Threshold { get; set; }

    [DataField(required: true)]
    public float ChanceOffset { get; set; }

    [DataField(required: true)]
    public float ChancePerDamage { get; set; }

    [DataField(required: true)]
    public float MinChance { get; set; }

    [DataField(required: true)]
    public float MaxChance { get; set; }

    /// <summary>
    /// Damage from which the trauma starts at the next level: the first value gives level 1, the second level 2, etc.
    /// </summary>
    [DataField]
    public float[] LevelThresholds { get; set; } = [];

    public TraumaRoll(float threshold, float chanceOffset, float chancePerDamage, float minChance, float maxChance) : this()
    {
        Threshold = threshold;
        ChanceOffset = chanceOffset;
        ChancePerDamage = chancePerDamage;
        MinChance = minChance;
        MaxChance = maxChance;
    }

    public readonly bool TryGetChance(float damage, out float chance)
    {
        chance = 0f;
        if (damage < Threshold)
            return false;

        chance = Math.Clamp((damage - ChanceOffset) * ChancePerDamage, MinChance, MaxChance);
        return true;
    }

    /// <summary>
    /// Level the trauma starts at for this damage, see <see cref="LevelThresholds"/>.
    /// </summary>
    public readonly int GetLevel(float damage)
    {
        var level = 0;
        foreach (var threshold in LevelThresholds)
        {
            if (damage >= threshold)
                level++;
        }

        return level;
    }
}
