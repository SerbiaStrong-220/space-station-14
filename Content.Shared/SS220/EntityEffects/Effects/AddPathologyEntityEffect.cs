// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.EntityEffects;
using Content.Shared.SS220.Pathology;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.EntityEffects.Effects;

/// <summary>
/// Adds a pathology or its stacks, e.g. liver damage from alcohol and poisons.
/// </summary>
public sealed partial class AddPathology : EntityEffectBase<AddPathology>
{
    [DataField(required: true)]
    public ProtoId<PathologyPrototype> Pathology;

    [DataField]
    public int Stacks = 1;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        if (!prototype.Resolve(Pathology, out var pathologyPrototype))
            return string.Empty;

        return Loc.GetString("reagent-effect-guidebook-ss220-add-pathology",
            ("chance", Probability),
            ("pathology", Loc.GetString(pathologyPrototype.Name)));
    }
}
