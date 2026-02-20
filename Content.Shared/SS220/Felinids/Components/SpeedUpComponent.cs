// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Nutrition.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Felinids.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SpeedUpComponent : Component
{
    /// <summary>
    ///     SpeedUp duration in seconds.
    /// </summary>
    [DataField]
    public float Duration = 10f;

    /// <summary>
    ///     The hunger threshold for using the SpeedUp ability.
    /// </summary>
    [DataField]
    public HungerThreshold HungerThreshold = HungerThreshold.Peckish;

    /// <summary>
    ///     The thirst threshold for using the SpeedUp ability.
    /// </summary>
    [DataField]
    public ThirstThreshold ThirstThreshold = ThirstThreshold.Thirsty;

    /// <summary>
    ///      The cost of a SpeedUp in hunger. A percentage of the maximum value.
    /// </summary>
    [DataField]
    public float HungerCost = 0.2f;

    /// <summary>
    ///      The cost of a SpeedUp in thirst. A percentage of the maximum value.
    /// </summary>
    [DataField]
    public float ThirstCost = 0.2f;

    [DataField]
    public float SpeedModifier = 1.3f;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan EndTime;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool Active = false;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public EntProtoId Action = "ActionSpeedUp";

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? ActionEntity;
}
