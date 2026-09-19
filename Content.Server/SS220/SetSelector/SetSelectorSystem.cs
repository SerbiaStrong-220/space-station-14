using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.SS220.SetSelector;
using Robust.Server.GameObjects;
using Robust.Server.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.SetSelector;

/// <summary>
/// <see cref="SetSelectorComponent"/>
/// this system links the interface to the logic, and will output to the player a set of items selected by him in the interface
/// </summary>
public sealed class SetSelectorSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SetSelectorComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<SetSelectorComponent, SetSelectorApproveMessage >(OnApprove);
        SubscribeLocalEvent<SetSelectorComponent, SetSelectorChangeSetMessage>(OnChangeSet);
    }

    private void OnUIOpened(Entity<SetSelectorComponent> selector, ref BoundUIOpenedEvent args)
    {
        UpdateUI(selector.Owner, selector.Comp);
    }

    private void OnApprove(Entity<SetSelectorComponent> selector, ref SetSelectorApproveMessage args)
    {
        if (selector.Comp.SelectedSets.Count != selector.Comp.MaxSelectedSets)
            return;

        EntityUid? spawnedStorage = null;
        if (selector.Comp.SpawnedStoragePrototype != null)
            spawnedStorage = Spawn(selector.Comp.SpawnedStoragePrototype, _transform.GetMapCoordinates(selector.Owner));

        foreach (var i in selector.Comp.SelectedSets)
        {
            var set = _proto.Index(selector.Comp.PossibleSets[i]);
            foreach (var item in set.Content)
            {
                var ent = Spawn(item, _transform.GetMapCoordinates(selector.Owner));
                if (TryComp<ItemComponent>(ent, out var itemComponent))
                {
                    if (spawnedStorage != null)
                        _storage.Insert(spawnedStorage.Value, ent, out _, playSound: false);
                    else
                        _transform.DropNextTo(ent, selector.Owner);
                }
            }
        }

        if (spawnedStorage != null)
            _hands.TryPickupAnyHand(args.Actor, spawnedStorage.Value);

        // Play the sound on coordinates of the backpack/toolbox. The reason being, since we immediately delete it, the sound gets deleted alongside it.
        _audio.PlayPvs(selector.Comp.ApproveSound, Transform(selector.Owner).Coordinates);
        QueueDel(selector);
    }
    private void OnChangeSet(Entity<SetSelectorComponent> selector, ref SetSelectorChangeSetMessage args)
    {
        //Swith selecting set
        if (!selector.Comp.SelectedSets.Remove(args.SetNumber))
            selector.Comp.SelectedSets.Add(args.SetNumber);

        UpdateUI(selector.Owner, selector.Comp);
    }

    private void UpdateUI(EntityUid uid, SetSelectorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        Dictionary<int, SelectableSetInfo> data = new();

        for (int i = 0; i < component.PossibleSets.Count; i++)
        {
            var set = _proto.Index(component.PossibleSets[i]);
            var selected = component.SelectedSets.Contains(i);
            var info = new SelectableSetInfo(
                set.Name,
                set.Description,
                set.Sprite,
                selected);
            data.Add(i, info);
        }

        _ui.SetUiState(uid, SetSelectorUIKey.Key, new SetSelectorBoundUserInterfaceState(data, component.MaxSelectedSets, component.ToolName, component.ToolDesc));
    }
}
