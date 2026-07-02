// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;


[RegisterComponent]
public sealed partial class HyperabsorptionComponent : Component
{
    /// <summary>Symptom this reads current stage from.</summary>
    [DataField]
    public ProtoId<PathologyPrototype> Pathology = "Hyperabsorption";

    /// <summary>Metabolic multiplyer per stage. By stage.</summary>
    [DataField]
    public List<float> SpeedBonus = new() { 0.1f, 0.2f, 0.3f };

    /// <summary>Set if component is being removed, so rate reverts.</summary>
    [ViewVariables]
    public bool Reverting;
}
