// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Humanoid;
using Content.Shared.Wieldable.Components;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.SS220.Wieldable;

public sealed partial class StrongMinerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StrongMinerComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
    }

    private void OnGetMeleeDamage(Entity<StrongMinerComponent> ent, ref GetMeleeDamageEvent args)
    {
        if (TryComp<WieldableComponent>(ent, out var wield) && wield.Wielded)
            return;

        if (!TryComp<HumanoidProfileComponent>(args.User, out var profile))
            return;

        if (ent.Comp.Species.Count > 0 && !ent.Comp.Species.Contains(profile.Species))
            return;

        if (!TryComp<IncreaseDamageOnWieldComponent>(ent, out var wieldBonus))
            return;

        args.Damage += wieldBonus.BonusDamage;
    }
}

