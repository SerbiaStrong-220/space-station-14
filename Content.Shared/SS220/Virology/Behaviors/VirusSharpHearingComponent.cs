// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Virology.Behaviors;

[RegisterComponent]
public sealed partial class VirusSharpHearingComponent : Component
{
    /// <summary>For later stage, force keen hearing (like ninja).</summary>
    [DataField]
    public bool ForceKeen;

    [DataField]
    public EntProtoId Action = "ActionToggleKeenHearing";

    [ViewVariables]
    public EntityUid? ActionEntity;

    [ViewVariables]
    public bool Reverting;
}
