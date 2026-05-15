// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
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
    private static readonly string ActiveBlockingOwnerLocale = "actively-blocking-attack";
    private static readonly string ActiveBlockingOthersLocale = "actively-blocking-others";

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    private bool _shouldReact = true;

    public override void Update(float frameTime)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var useKey = ContentKeyFunctions.ToggleActiveBlocking;

        if (_inputSystem.CmdStates.GetState(useKey) == BoundKeyState.Up)
        {
            if (!_shouldReact)
                _shouldReact = true;
            return;
        }

        var entityNull = _player.LocalEntity;

        if (entityNull is not { } entity || !TryComp<AltBlockingUserComponent>(entity, out var userComp))
            return;

        if (!_shouldReact)
            return;

        var msgUser = Loc.GetString(ActiveBlockingOwnerLocale);
        var msgOther = Loc.GetString(ActiveBlockingOthersLocale, ("blockerName", entity));
        _popupSystem.PopupPredicted(msgUser, msgOther, entity, entity);

        RaiseNetworkEvent(new BlockAttemptEvent { User = GetNetEntity(entity) });
        _shouldReact = false;
        return;
    }
}
