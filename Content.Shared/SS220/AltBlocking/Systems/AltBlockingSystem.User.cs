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

    private void OnParentChanged(EntityUid uid, AltBlockingUserComponent component, ref EntParentChangedMessage args)
    {
        UserStopBlocking(uid, component);
    }

    private void OnInsertAttempt(EntityUid uid, AltBlockingUserComponent component, ContainerGettingInsertedAttemptEvent args)
    {
        UserStopBlocking(uid, component);
    }

    private void OnMapInit(EntityUid uid, AltBlockingUserComponent component, MapInitEvent args)
    {
        _actionContainer.EnsureAction(uid, ref component.BlockingToggleActionEntity, component.BlockingToggleAction);
        Dirty(uid, component);
    }

    private void OnToggleAction(EntityUid uid, AltBlockingUserComponent component, ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        var handQuery = GetEntityQuery<HandsComponent>();

        if (!handQuery.TryGetComponent(args.Performer, out var hands))
            return;

        if (component.IsBlocking)
            StopBlocking(component, args.Performer);
        else
            StartBlocking(component, args.Performer);
        Dirty(uid, component);
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

    /// <summary>
    /// Check for the shield and has the user stop blocking
    /// Used where you'd like the user to stop blocking, but also don't want to remove the <see cref="AltBlockingUserComponent"/>
    /// </summary>
    /// <param name="uid">The user blocking</param>
    /// <param name="component">The <see cref="AltBlockingUserComponent"/></param>
    private void UserStopBlocking(EntityUid uid, AltBlockingUserComponent component)
    {
        foreach (var shield in component.BlockingItemsShields)
        {
            if (TryComp<AltBlockingComponent>(shield, out var blockComp) && blockComp.IsBlocking)
                StopBlocking(component, uid);
        }
    }
}
