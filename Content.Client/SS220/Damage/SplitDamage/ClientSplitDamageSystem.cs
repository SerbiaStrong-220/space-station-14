using Content.Shared.SS220.Damage.SplitDamage;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Client.SS220.Damage.SplitDamage;

public sealed class ClientSplitDamageSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<LightAttackEvent>(OnLightAttack);
        SubscribeLocalEvent<HeavyAttackEvent>(OnHeavyAttack);
    }

    private void OnLightAttack(LightAttackEvent ev, EntitySessionEventArgs args)
    {
        if (!HasComp<SplitDamageComponent>(GetEntity(ev.Weapon)))
            return;

        RaiseNetworkEvent(new SplitDamageEvent(false, ev.Weapon));
    }

    private void OnHeavyAttack(HeavyAttackEvent ev, EntitySessionEventArgs args)
    {
        if (!HasComp<SplitDamageComponent>(GetEntity(ev.Weapon)))
            return;

        RaiseNetworkEvent(new SplitDamageEvent(true, ev.Weapon));
    }
}
