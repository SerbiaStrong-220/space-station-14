// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.SS220.Weapons.Ranged.Events;
using Content.Shared.Hands.Components;
using Content.Shared.Toggleable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Shared.SS220.AltBlocking;

public sealed partial class SharedAltBlockingSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    private void InitializeUser()
    {
        SubscribeLocalEvent<AltBlockingUserComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AltBlockingUserComponent, ToggleActionEvent>(OnToggleAction);
        SubscribeLocalEvent<AltBlockingUserComponent, EntityTerminatingEvent>(OnEntityTerminating);
        SubscribeAllEvent<BlockAttemptEvent>(OnBlockToggleAttempt);
    }

    private void OnMapInit(Entity<AltBlockingUserComponent> ent, ref MapInitEvent args)
    {
        _actionContainer.EnsureAction(ent.Owner, ref ent.Comp.BlockingToggleActionEntity, ent.Comp.BlockingToggleAction);
        Dirty(ent.Owner, ent.Comp);
    }

    private void OnToggleAction(Entity<AltBlockingUserComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        var handQuery = GetEntityQuery<HandsComponent>();

        if (!handQuery.TryGetComponent(args.Performer, out var hands))
            return;

        if (ent.Comp.IsBlocking)
            StopBlocking(ent.Comp, args.Performer);

        else
            StartBlocking(ent.Comp, args.Performer);

        Dirty(ent.Owner, ent.Comp);
        args.Handled = true;
    }

    private void OnBlockToggleAttempt(BlockAttemptEvent args)
    {
        if (args.Handled)
            return;

        if (!TryGetEntity(args.User, out var localUser))
            return;

        if (!TryComp<AltBlockingUserComponent>(localUser, out var blockingUserComp))
            return;

        var handQuery = GetEntityQuery<HandsComponent>();

        if (!handQuery.TryGetComponent(localUser, out var hands))
            return;

        if (blockingUserComp.IsBlocking)
            StopBlocking(blockingUserComp, (EntityUid)localUser);

        else
            StartBlocking(blockingUserComp, (EntityUid)localUser);

        Dirty((EntityUid)localUser, blockingUserComp);
        args.Handled = true;
    }

    private void OnEntityTerminating(Entity<AltBlockingUserComponent> ent, ref EntityTerminatingEvent args)
    {
        StopBlocking(ent.Comp, ent.Owner);
        if (_net.IsServer)
        {
            _actionsSystem.RemoveAction(ent.Comp.BlockingToggleActionEntity);
            RemComp<AltBlockingUserComponent>(ent.Owner);
        }
    }
}
