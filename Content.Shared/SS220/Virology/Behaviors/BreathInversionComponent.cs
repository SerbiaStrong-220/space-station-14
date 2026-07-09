// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Metabolism;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Virology.Behaviors;

[RegisterComponent]
public sealed partial class BreathInversionComponent : Component
{
    /// <summary>What each race's breathing inverts to, by the lung's metabolizer type.</summary>
    [DataField]
    public Dictionary<ProtoId<MetabolizerTypePrototype>, ProtoId<MetabolizerTypePrototype>> InvertTo = new()
    {
        ["Human"] = "Vox",     // human, dwarf
        ["Animal"] = "Vox",    // Unathi, tajaran
        ["Arachnid"] = "Vox",
        ["Moth"] = "Vox",
        ["Plant"] = "Vox",
        ["Vox"] = "Human",
        ["Slime"] = "Human",
    };

    /// <summary>Each affected lung's original metabolizer types, to restore on cure.</summary>
    [ViewVariables]
    public Dictionary<EntityUid, HashSet<ProtoId<MetabolizerTypePrototype>>> Original = [];
}
