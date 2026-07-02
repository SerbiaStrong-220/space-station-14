// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

/// <summary>
/// Generic symptom behaviour: scales incoming damage by a <see cref="DamageModifierSet"/>, optionally
/// changing with the pathology's stage. Compose it onto any resistance/vulnerability symptom in YAML —
/// no bespoke system required.
/// </summary>
[RegisterComponent]
public sealed partial class PathologyDamageModifierComponent : Component
{
    /// <summary>Pathology to read the current stage from. Null keeps the first modifier at every stage.</summary>
    [DataField]
    public ProtoId<PathologyPrototype>? Pathology;

    /// <summary>Damage modifier per stage (clamped to the last entry). A single entry = same at every stage.</summary>
    [DataField(required: true)]
    public List<DamageModifierSet> ModifierPerStage = new();
}
