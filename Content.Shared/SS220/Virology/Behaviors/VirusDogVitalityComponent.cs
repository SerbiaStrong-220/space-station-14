// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Virology.Behaviors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VirusDogVitalityComponent : Component
{
    /// <summary>Extra crit threshold added on top of the base one (additive, stacks with other modifiers).</summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Threshold;

    /// <summary>Additional offset applied to death threshold beyond crit one.</summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 DeathThresholdOffset;

    /// <summary>Set when removed so modifier refresh and drops bonus.</summary>
    [ViewVariables]
    public bool Reverting;
}
