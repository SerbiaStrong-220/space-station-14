// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.Input;
using Content.Shared.Popups;
using Content.Shared.SS220.AltBlocking;
using Content.Shared.SS220.Weapons.Ranged.Events;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Timing;

namespace Content.Client.SS220.AltBlocking;

public sealed partial class AltBlockingInputSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    private bool ShouldReact = true;

    public override void Update(float frameTime)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var useKey = ContentKeyFunctions.ToggleActiveBlocking;

        if (_inputSystem.CmdStates.GetState(useKey) == BoundKeyState.Up)
        {
            if (!ShouldReact)
                ShouldReact = true;
            return;
        }

        var entityNull = _player.LocalEntity;

        if (entityNull == null || !TryComp<AltBlockingUserComponent>(entityNull, out var userComp))
            return;

        var entity = entityNull.Value;

        if (!ShouldReact)
            return;

        var msgUser = Loc.GetString("actively-blocking-attack");
        var msgOther = Loc.GetString("actively-blocking-others", ("blockerName", entity));
        _popupSystem.PopupPredicted(msgUser, msgOther, entity, entity);

        RaiseNetworkEvent(new BlockAttemptEvent { User = GetNetEntity(entity) });
        ShouldReact = false;
        return;
    }
}
