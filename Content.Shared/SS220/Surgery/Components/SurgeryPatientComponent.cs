// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Surgery.Graph;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Components;

/// <summary>
/// This component contains information about being a valid target for surgery and ongiong operations
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class SurgeryPatientComponent : Component
{
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<ProtoId<SurgeryGraphPrototype>, string> OngoingSurgeries = new();

    [DataField]
    public float OnSurgeryMoveBleed = 0.2f;

    /// <summary>
    /// Added to the step failure chance when the patient feels pain.
    /// </summary>
    [DataField]
    public float UnanesthetizedFailurePenalty = 0.25f;

    /// <summary>
    /// Added to the step failure chance when the patient is buckled to something other than an operating table, e.g. a bed.
    /// </summary>
    [DataField]
    public float NoOperatingTableFailurePenalty = 0.2f;

    [DataField]
    public float NoOperatingTableDelayMultiplier = 1.35f;

    /// <summary>
    /// Added to the step failure chance when the patient is not buckled at all.
    /// </summary>
    [DataField]
    public float FloorFailurePenalty = 0.35f;

    [DataField]
    public float FloorDelayMultiplier = 1.5f;

    /// <summary>
    /// Added to the step failure chance when the step is done with an improvised tool.
    /// </summary>
    [DataField]
    public float ImprovisedToolFailurePenalty = 0.25f;

    [DataField]
    public float ImprovisedToolDelayMultiplier = 1.6f;

    /// <summary>
    /// Step delay in seconds that no modifiers can go below.
    /// </summary>
    [DataField]
    public float MinStepDelay = 0.5f;

    /// <summary>
    /// Step failure chance that no modifiers can go above, so a step always has a chance to succeed.
    /// </summary>
    [DataField]
    public float MaxStepFailureChance = 0.95f;
}
