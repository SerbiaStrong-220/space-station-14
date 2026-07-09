// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.SS220.Virology.Behaviors;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable.Components;

namespace Content.Server.SS220.Virology.Behaviors;

public sealed partial class RecoilWeaknessSystem : EntitySystem
{
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private GunSystem _gun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RecoilWeaknessComponent, ShooterImpulseEvent>(OnShooterImpulse);
    }

    private void OnShooterImpulse(Entity<RecoilWeaknessComponent> ent, ref ShooterImpulseEvent args)
    {
        // only a two-handed weapon actually held in both hands trigger this
        if (!_gun.TryGetGun(ent.Owner, out var gun)
            || !TryComp<WieldableComponent>(gun, out var wieldable)
            || !wieldable.Wielded)
            return;

        _stun.TryKnockdown(ent.Owner, ent.Comp.KnockdownTime, drop: false);
    }
}
