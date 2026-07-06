// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Metabolism;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Virology.Behaviors;

[RegisterComponent]
public sealed partial class BreathInversionComponent : Component
{
    /// <summary>Lung type a former oxygen-breather is switched to nitrogen.</summary>
    [DataField]
    public ProtoId<MetabolizerTypePrototype> NitrogenBreatherType = "Vox";

    /// <summary>Lung type a former nitrogen-breather is switched to oxygen.</summary>
    [DataField]
    public ProtoId<MetabolizerTypePrototype> OxygenBreatherType = "Human";

    /// <summary>Host with any of these inverts to an oxygen-breather instead.</summary>
    [DataField]
    public HashSet<ProtoId<MetabolizerTypePrototype>> NitrogenBreathers = ["Vox", "Slime"];

    /// <summary>Each affected lung's original metabolizer types, to restore on cure.</summary>
    [ViewVariables]
    public Dictionary<EntityUid, HashSet<ProtoId<MetabolizerTypePrototype>>> Original = [];
}
