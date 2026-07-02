// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

[RegisterComponent]
public sealed partial class NoodleArmsComponent : Component
{
    /// <summary>The symptom this reads the current stage from.</summary>
    [DataField]
    public ProtoId<PathologyPrototype> Pathology = "NoodleArms";

    /// <summary>Minimum gap between spasms.</summary>
    [DataField]
    public TimeSpan MinInterval = TimeSpan.FromSeconds(15);

    /// <summary>Maximum gap between spasms.</summary>
    [DataField]
    public TimeSpan MaxInterval = TimeSpan.FromSeconds(45);

    /// <summary>When the next spasm is due.</summary>
    [ViewVariables]
    public TimeSpan? NextSpasm;
}
