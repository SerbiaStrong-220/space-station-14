using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.SS220.SetSelector;
using Content.Shared.Storage;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.SetSelector;

/// <summary>
/// Spawns the sets chosen in <see cref="SetSelectorComponent"/> once the selection is approved.
/// </summary>
public sealed partial class SetSelectorSystem : SharedSetSelectorSystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SetSelectorComponent, SetSelectorApproveMessage>(OnApprove);
    }

    private void OnApprove(Entity<SetSelectorComponent> selector, ref SetSelectorApproveMessage args)
    {
        if (EntityManager.IsQueuedForDeletion(selector))
            return;

        if (selector.Comp.SelectedSets.Count != selector.Comp.MaxSelectedSets)
        {
            _popup.PopupEntity(Loc.GetString("set-selector-approve-failed"), selector, args.Actor);
            return;
        }

        EntityUid? spawnedStorage = null;
        if (selector.Comp.SpawnedStoragePrototype is { } storageProto)
            spawnedStorage = SpawnNextToOrDrop(storageProto, selector);

        foreach (var index in selector.Comp.SelectedSets)
        {
            foreach (var item in _proto.Index(selector.Comp.PossibleSets[index]).Content)
            {
                if (spawnedStorage is { } storage)
                    SpawnInContainerOrDrop(item, storage, StorageComponent.ContainerId);
                else
                    SpawnNextToOrDrop(item, selector);
            }
        }

        if (spawnedStorage is { } spawned)
        {
            if (_hands.IsHolding(args.Actor, selector.Owner))
                _hands.TryDrop(args.Actor, selector.Owner, checkActionBlocker: false, doDropInteraction: false, suppressDropSound: true);

            _hands.TryPickupAnyHand(args.Actor, spawned);
        }

        _audio.PlayPvs(selector.Comp.ApproveSound, Transform(selector.Owner).Coordinates);
        QueueDel(selector);
    }
}
