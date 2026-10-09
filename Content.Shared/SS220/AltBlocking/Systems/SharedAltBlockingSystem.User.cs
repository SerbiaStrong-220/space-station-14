// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.Hands.Components;
using Content.Shared.Input;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Shared.SS220.AltBlocking;

public sealed partial class SharedAltBlockingSystem
{
    [Dependency] private SharedAudioSystem _audio = default!;

    private void InitializeUser()
    {
        SubscribeLocalEvent<AltBlockingUserComponent, EntityTerminatingEvent>(OnEntityTerminating);

        SubscribeLocalEvent<AltBlockingUserComponent, ThrowAttemptEvent>(OnThrowAttempt);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ToggleActiveBlocking, InputCmdHandler.FromDelegate(OnBlockToggleAttempt, handle: false, outsidePrediction: false))
            .Register<SharedAltBlockingSystem>();
    }


    private void OnBlockToggleAttempt(ICommonSession? session)
    {
        if (session is not { } playerSession)
            return;

        if (playerSession.AttachedEntity is not { Valid: true } user)
            return;

        if (!TryComp<AltBlockingUserComponent>(user, out var blockingUserComp))
            return;

        if (!TryComp<HandsComponent>(user, out var handsComp))
            return;

        if (blockingUserComp.Blocking)
            StopBlocking((user, blockingUserComp));

        else
            TryStartBlocking((user, blockingUserComp));

        Dirty(user, blockingUserComp);
    }

    private void OnThrowAttempt(Entity<AltBlockingUserComponent> ent, ref ThrowAttemptEvent args)
    {
        if (!ent.Comp.Blocking)
            return;

        _popupSystem.PopupEntity(Loc.GetString(BlockThrowingLocale), ent);//client doesn't catch the event somewhy

        args.Cancel();
    }

    private void OnEntityTerminating(Entity<AltBlockingUserComponent> ent, ref EntityTerminatingEvent args)
    {
        StopBlocking(ent);
        if (_net.IsServer)
            RemComp<AltBlockingUserComponent>(ent.Owner);
    }
}
