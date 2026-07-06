// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Virology.Behaviors;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable.Components;

namespace Content.Server.SS220.Virology.Behaviors;

public sealed partial class RecoilWeaknessSystem : EntitySystem
{
    [Dependency] private SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunComponent, GunShotEvent>(OnGunShot);
    }

    private void OnGunShot(Entity<GunComponent> gun, ref GunShotEvent args)
    {
        if (!TryComp<RecoilWeaknessComponent>(args.User, out var comp))
            return;

        // only a two-handed weapon actually held in both hands
        if (!TryComp<WieldableComponent>(gun, out var wieldable) || !wieldable.Wielded)
            return;

        _stun.TryKnockdown(args.User, comp.KnockdownTime, drop: false);
    }
}
