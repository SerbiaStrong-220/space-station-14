// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.SS220.Weapons.Ranged.Events;
using Content.Shared.Hands.Components;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.SS220.AltBlocking;

public sealed partial class SharedAltBlockingSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    private void InitializeUser()
    {
        SubscribeLocalEvent<AltBlockingUserComponent, EntityTerminatingEvent>(OnEntityTerminating);
        SubscribeAllEvent<BlockAttemptEvent>(OnBlockToggleAttempt);
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
            StopBlocking(((EntityUid)localUser, blockingUserComp));

        else
            StartBlocking(((EntityUid)localUser, blockingUserComp));

        Dirty((EntityUid)localUser, blockingUserComp);
        args.Handled = true;
    }

    private void OnEntityTerminating(Entity<AltBlockingUserComponent> ent, ref EntityTerminatingEvent args)
    {
        StopBlocking(ent);
        if (_net.IsServer)
            RemComp<AltBlockingUserComponent>(ent.Owner);
    }
}
