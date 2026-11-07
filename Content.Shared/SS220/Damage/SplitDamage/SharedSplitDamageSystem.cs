using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Damage.SplitDamage;

public sealed class SharedSplitDamageSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<SplitDamageComponent, GetMeleeDamageEvent>(OnSplitDamage);
        SubscribeNetworkEvent<SplitDamageEvent>(OnAttack);
    }

    private void OnSplitDamage(Entity<SplitDamageComponent> ent, ref GetMeleeDamageEvent args)
    {
        args.Damage = ent.Comp.WideAttack ? ent.Comp.WideDamage : ent.Comp.PunchDamage;
    }

    private void OnAttack(SplitDamageEvent args)
    {
        if (!TryComp<SplitDamageComponent>(GetEntity(args.Weapon), out var splitDamageComponent))
            return;

        splitDamageComponent.WideAttack = args.IsWideAttack;
    }
}

[Serializable]
[NetSerializable]
public sealed class SplitDamageEvent : EntityEventArgs
{
    public bool IsWideAttack;
    public NetEntity Weapon;

    public SplitDamageEvent(bool isWideAttack, NetEntity weapon)
    {
        IsWideAttack = isWideAttack;
        Weapon = weapon;
    }
}
