// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;

namespace Content.Shared.SS220.Virology.Effects;

public sealed partial class VirusDamageEffect : IVirusEffect
{
    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    /// <summary>If true, damage only applies while the host is lying down or buckled.</summary>
    [DataField]
    public bool WhileRecumbent;

    /// <summary>If set, damage only applies while this inventory slot is occupied.</summary>
    [DataField]
    public string? RequireSlot;

    public void ApplyEffect(in VirusProgressArgs args)
    {
        if (WhileRecumbent && !VirusEffectConditions.IsRecumbent(args.Carrier, args.EntityManager))
            return;

        if (RequireSlot != null && !args.EntityManager.System<InventorySystem>().TryGetSlotEntity(args.Carrier, RequireSlot, out _))
            return;

        args.EntityManager.System<DamageableSystem>().TryChangeDamage(args.Carrier, Damage);
    }
}
