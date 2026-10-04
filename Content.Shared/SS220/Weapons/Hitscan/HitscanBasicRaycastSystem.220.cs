// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Damage.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.SS220.Weapons.Components;
using Content.Shared.Standing;

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed partial class HitscanBasicRaycastSystem : EntitySystem
{

    //SS220 weapon overhaul begin
    private bool ShouldIgnoreRequireTarget(EntityUid target, EntityUid gun, EntityUid user)
    {
        if (!TryComp<RequireProjectileTargetComponent>(target, out var requireTargetComp))
            return false;

        if (!TryComp<MobStateComponent>(target, out var statesComp) || (statesComp.CurrentState != Mobs.MobState.Alive))
            return false;

        if (TryComp<StandingStateComponent>(user, out var standingState) && _standing.IsDown((user, standingState)))
            if (TryComp<StandingStateComponent>(target, out var standingStateTarget) && _standing.IsDown((target, standingStateTarget)))
                return true;

        if (!TryComp<GunAimableComponent>(gun, out var aimableComp) || !aimableComp.IsAimed)
            return false;

        return true;
    }
    //SS220 weapon overhaul end
}
