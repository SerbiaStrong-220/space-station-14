// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.SS220.Pathology;

namespace Content.Server.SS220.Pathology;

/// <summary>
/// Applies a symptom's <see cref="PathologyDamageModifierComponent"/> to incoming damage, picking the
/// modifier for the current stage. One reusable system for every resistance/vulnerability symptom.
/// </summary>
public sealed partial class PathologyDamageModifierSystem : EntitySystem
{
    [Dependency] private SharedPathologySystem _pathology = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PathologyDamageModifierComponent, DamageModifyEvent>(OnDamageModify);
    }

    private void OnDamageModify(Entity<PathologyDamageModifierComponent> ent, ref DamageModifyEvent args)
    {
        if (ent.Comp.ModifierPerStage.Count == 0)
            return;

        var modifier = ent.Comp.Pathology is { } pathology
            && _pathology.TryGetStageValue(ent.Owner, pathology, ent.Comp.ModifierPerStage, out var staged)
            ? staged
            : ent.Comp.ModifierPerStage[0];

        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, modifier);
    }
}
