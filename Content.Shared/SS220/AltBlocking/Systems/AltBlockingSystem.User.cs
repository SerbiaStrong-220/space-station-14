// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.Toggleable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Shared.SS220.AltBlocking;

public sealed partial class AltBlockingSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    private void InitializeUser()
    {
        SubscribeLocalEvent<AltBlockingUserComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AltBlockingUserComponent, ToggleActionEvent>(OnToggleAction);
        SubscribeLocalEvent<AltBlockingUserComponent, EntityTerminatingEvent>(OnEntityTerminating);
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

    private void OnEntityTerminating(EntityUid uid, AltBlockingUserComponent component, ref EntityTerminatingEvent args)
    {
        StopBlocking(component, uid);
        if (_net.IsServer)
        {
            _actionsSystem.RemoveAction(component.BlockingToggleActionEntity);
            RemComp<AltBlockingUserComponent>(uid);
        }
    }
}
