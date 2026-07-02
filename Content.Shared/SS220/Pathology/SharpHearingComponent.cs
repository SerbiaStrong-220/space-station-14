// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.IgnoreLightVision.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

[RegisterComponent]
public sealed partial class SharpHearingComponent : Component
{
    /// <summary>The symptom this reads the current stage from.</summary>
    [DataField]
    public ProtoId<PathologyPrototype> Pathology = "SharpHearing";

    /// <summary>Tajara keen-hearing toggle action granted (stage 1).</summary>
    [DataField]
    public EntProtoId Action = "ActionToggleKeenHearing";

    [ViewVariables]
    public EntityUid? ActionEntity;

    /// <summary>
    /// Snapshot of the host's keen-hearing overlay state, taken before the late stage forces it.
    /// A host that already had keen hearing (e.g. a Tajara) gets its original state back on cure.
    /// </summary>
    [ViewVariables]
    public bool CapturedKeenHearing;

    [ViewVariables]
    public IgnoreLightVisionOverlayState OriginalKeenState;

    [ViewVariables]
    public TimeSpan? OriginalKeenToggleTime;
}
