// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.FCB.AltBlocking;
using Content.Shared.FCB.Weapons.Ranged.Events;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Timing;

namespace Content.Client.FCB.AltBlocking;

public partial class AltBlockingInputSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private bool ShouldReact = true;

    public override void Update(float frameTime)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var useKey = ContentKeyFunctions.ToggleActiveBlocking;

        if (_inputSystem.CmdStates.GetState(useKey) == BoundKeyState.Up)
        {
            ShouldReact = true;
            return;
        }

        var entityNull = _player.LocalEntity;

        if (entityNull == null || !TryComp<AltBlockingUserComponent>(entityNull, out var userComp))
            return;

        var entity = entityNull.Value;

        if (!ShouldReact)
            return;

        RaisePredictiveEvent(new BlockAttemptEvent { User = GetNetEntity(entity) });
        ShouldReact = false;
        return;
    }
}
