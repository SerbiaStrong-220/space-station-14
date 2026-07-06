// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;

namespace Content.Shared.SS220.Virology.Behaviors;

[RegisterComponent]
public sealed partial class VirusDogVitalityComponent : Component
{
    /// <summary>Extra crit threshold for current stage(this isn't stacking with chems like rubicon).</summary>
    [DataField]
    public FixedPoint2 Threshold;

    /// <summary>Additional offset applied to death threshold beyond crit one.</summary>
    [DataField]
    public FixedPoint2 DeathThresholdOffset;

    /// <summary>Set when removed so modifier refresh and drops bonus.</summary>
    [ViewVariables]
    public bool Reverting;
}
