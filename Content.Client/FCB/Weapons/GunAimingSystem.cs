//© FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Client.Weapons.Ranged.Systems;
using Content.Shared.CombatMode;
using Content.Shared.FCB.Weapons.Components;
using Content.Shared.FCB.Weapons.Ranged.Events;
using Content.Shared.FCB.Weapons.Ranged.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Timing;

namespace Content.Client.FCB.Weapons;

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

        if (!_gun.TryGetGun(entity, out var gunUid, out var gun) || !gun.UseKey)
            return;

        if (!TryComp<GunAimableComponent>(gunUid, out var aimableComp))
            return;

        var useKey = EngineKeyFunctions.UseSecondary;

        if (_inputSystem.CmdStates.GetState(useKey) == BoundKeyState.Down && !aimableComp.IsAimed)
        {
            RaisePredictiveEvent(new AimStatusChangeAttemptEvent { Gun = GetNetEntity(gunUid), Aim = true, User = GetNetEntity(entity) });
            return;
        }

        if (_inputSystem.CmdStates.GetState(useKey) == BoundKeyState.Up && aimableComp.IsAimed)
            RaisePredictiveEvent(new AimStatusChangeAttemptEvent { Gun = GetNetEntity(gunUid), Aim = false, User = GetNetEntity(entity) });
    }
}
