// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.
using Content.Client.Weapons.Ranged.Systems;
using Content.Shared.CombatMode;
using Content.Shared.SS220.Weapons.Components;
using Content.Shared.SS220.Weapons.Ranged.Systems;
using Content.Shared.SS220.Weapons.Ranged.Events;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Timing;

namespace Content.Client.SS220.Weapons;

public sealed partial class GunAimingSystem : SharedGunAimingSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly GunSystem _gun = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var entityNull = _player.LocalEntity;

        if (entityNull == null || !TryComp<CombatModeComponent>(entityNull, out var combatComp) || !combatComp.IsInCombatMode)
            return;

        var entity = entityNull.Value;

        if (!_gun.TryGetGun(entity, out var gun) || !gun.Comp.UseKey)
            return;

        if (!TryComp<GunAimableComponent>(gun.Owner, out var aimableComp))
            return;

        var useKey = EngineKeyFunctions.UseSecondary;

        if (_inputSystem.CmdStates.GetState(useKey) == BoundKeyState.Down && !aimableComp.IsAimed)
        {
            RaisePredictiveEvent(new AimStatusChangeAttemptEvent { Gun = GetNetEntity(gun.Owner), Aim = true, User = GetNetEntity(entity) });
            return;
        }

        if (_inputSystem.CmdStates.GetState(useKey) == BoundKeyState.Up && aimableComp.IsAimed)
            RaisePredictiveEvent(new AimStatusChangeAttemptEvent { Gun = GetNetEntity(gun.Owner), Aim = false, User = GetNetEntity(entity) });
    }
}
