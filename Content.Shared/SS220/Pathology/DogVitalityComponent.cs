// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

[RegisterComponent]
public sealed partial class DogVitalityComponent : Component
{
    /// <summary>The symptom this reads the current stage from.</summary>
    [DataField]
    public ProtoId<PathologyPrototype> Pathology = "DogVitality";

    /// <summary>Crit threshold per stage.</summary>
    [DataField]
    public List<FixedPoint2> Thresholds = new() { 110, 120, 130 };

    /// <summary>Death threshold so this isnt "bugged".</summary>
    [DataField]
    public FixedPoint2 DeathThresholdOffset = 1;

    /// <summary>Set while the component is being removed, so the refresh drops our modifier and reverts to base.</summary>
    [ViewVariables]
    public bool Reverting;
}
