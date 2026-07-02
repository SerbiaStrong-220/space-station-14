// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;

namespace Content.Shared.SS220.Pathology.Effects;

/// <summary>
/// Deals damage (or heals, with negatives) each tick. Optional gates: only while the host is recumbent
/// (down/buckled) and/or while a given inventory slot is occupied. With no gates it always applies.
/// </summary>
public sealed partial class PathologyDamageEffect : IPathologyEffect
{
    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    /// <summary>If true, the damage only applies while the host is lying down or buckled.</summary>
    [DataField]
    public bool WhileRecumbent;

    /// <summary>If set, the damage only applies while this inventory slot is occupied (e.g. "mask").</summary>
    [DataField]
    public string? RequireSlot;

    public void ApplyEffect(in PathologyEffectArgs args)
    {
        if (WhileRecumbent && !PathologyEffectConditions.IsRecumbent(args.Target, args.EntityManager))
            return;

        if (RequireSlot != null && !args.EntityManager.System<InventorySystem>().TryGetSlotEntity(args.Target, RequireSlot, out _))
            return;

        args.EntityManager.System<DamageableSystem>().TryChangeDamage(args.Target, Damage);
    }
}
