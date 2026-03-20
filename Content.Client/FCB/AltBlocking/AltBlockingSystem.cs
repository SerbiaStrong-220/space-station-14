// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Robust.Shared.Input;
using Robust.Shared.Timing;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Shared.FCB.AltBlocking;

public partial class AltBlockingSystem : SharedAltBlockingSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Update(float frameTime)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var entityNull = _player.LocalEntity;

        if (entityNull == null || !TryComp<AltBlockingUserComponent>(entityNull, out var userComp))
            return;

        var entity = entityNull.Value;

        var useKey = EngineKeyFunctions.UseSecondary;

        if (_inputSystem.CmdStates.GetState(useKey) == BoundKeyState.Down && !userComp.IsBlocking)
        {
            RaisePredictiveEvent(new AimStatusChangeAttemptEvent { Gun = GetNetEntity(gunUid), Aim = true, User = GetNetEntity(entity) });
            return;
        }

        if (_inputSystem.CmdStates.GetState(useKey) == BoundKeyState.Up && userComp.IsBlocking)
            RaisePredictiveEvent(new AimStatusChangeAttemptEvent { Gun = GetNetEntity(gunUid), Aim = false, User = GetNetEntity(entity) });
    }
}
