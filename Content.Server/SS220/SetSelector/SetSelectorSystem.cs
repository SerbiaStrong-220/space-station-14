using Content.Shared.Hands.EntitySystems;
using Content.Shared.SS220.SetSelector;
using Content.Shared.Storage;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.SetSelector;

/// <summary>
/// <see cref="SetSelectorComponent"/>
/// this system links the interface to the logic, and will output to the player a set of items selected by him in the interface
/// </summary>
public sealed partial class SetSelectorSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SetSelectorComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<SetSelectorComponent, SetSelectorApproveMessage>(OnApprove);
        SubscribeLocalEvent<SetSelectorComponent, SetSelectorChangeSetMessage>(OnChangeSet);
    }

    private void OnUIOpened(Entity<SetSelectorComponent> selector, ref BoundUIOpenedEvent args)
    {
        UpdateUI(selector);
    }

    private void OnApprove(Entity<SetSelectorComponent> selector, ref SetSelectorApproveMessage args)
    {
        if (selector.Comp.SelectedSets.Count != selector.Comp.MaxSelectedSets || EntityManager.IsQueuedForDeletion(selector))
            return;

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
            _hands.TryPickupAnyHand(args.Actor, spawned);

        // Play the sound on coordinates of the backpack/toolbox. The reason being, since we immediately delete it, the sound gets deleted alongside it.
        _audio.PlayPvs(selector.Comp.ApproveSound, Transform(selector.Owner).Coordinates);
        QueueDel(selector);
    }

    private void OnChangeSet(Entity<SetSelectorComponent> selector, ref SetSelectorChangeSetMessage args)
    {
        if (args.SetNumber < 0 || args.SetNumber >= selector.Comp.PossibleSets.Count)
            return;

        if (!selector.Comp.SelectedSets.Remove(args.SetNumber))
            selector.Comp.SelectedSets.Add(args.SetNumber);

        UpdateUI(selector);
    }

    private void UpdateUI(Entity<SetSelectorComponent> selector)
    {
        var state = new SetSelectorBoundUserInterfaceState(
            new(selector.Comp.PossibleSets),
            new(selector.Comp.SelectedSets),
            selector.Comp.MaxSelectedSets,
            selector.Comp.ToolName,
            selector.Comp.ToolDesc);

        _ui.SetUiState(selector.Owner, SetSelectorUIKey.Key, state);
    }
}
