using System.Diagnostics;
using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.SS220.BeerUpdate.MaltGrinder;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server.SS220.BeerUpdate.MaltGrinder;

public sealed class MaltGrinderSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MaltGrinderComponent, MaltGrinderStartMessage>(OnStart);
        SubscribeLocalEvent<MaltGrinderComponent, MaltGrinderEjectChamberAllMessage>(OnEjectAll);
        SubscribeLocalEvent<MaltGrinderComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<MaltGrinderComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<MaltGrinderComponent, ContainerIsRemovingAttemptEvent>(OnContainerRemovingAttempt);
        SubscribeLocalEvent<MaltGrinderComponent, ComponentStartup>((uid, _, _) => UpdateUiState(uid));
        SubscribeLocalEvent<MaltGrinderComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<MaltGrinderComponent, EntRemovedFromContainerMessage>(OnContainerModified);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveMaltGrinderComponent, MaltGrinderComponent>();
        while (query.MoveNext(out var uid, out var active, out var maltGrinder))
        {
            if (_timing.CurTime < active.EndTime)
                continue;

            ProceedWork(uid, maltGrinder);
        }
    }

    private void OnExamine(Entity<MaltGrinderComponent> entity, ref ExaminedEvent args)
    {
        var inputContainer = _containerSystem.EnsureContainer<Container>(entity, SharedMaltGrinder.InputContainerId);
        var outputContainer = _itemSlotSystem.GetItemOrNull(entity, SharedMaltGrinder.BeakerSlotId);

        if (inputContainer.ContainedEntities.Count > 0)
        {
            args.PushMarkup(Loc.GetString("malt-grinder-examine-input"));
        }
        else
        {
            args.PushMarkup(Loc.GetString("malt-grinder-examine-input-empty"));
        }

        if (outputContainer.HasValue)
        {
            args.PushMarkup(Loc.GetString("malt-grinder-examine-output"));
        }
        else
        {
            args.PushMarkup(Loc.GetString("malt-grinder-examine-output-empty"));
        }
    }

    private void OnStart(Entity<MaltGrinderComponent> entity, ref MaltGrinderStartMessage args)
    {
        if (TryComp(entity, out ActiveMaltGrinderComponent? active))
        {
            return;
        }

        var inputContainer = _containerSystem.EnsureContainer<Container>(entity, SharedMaltGrinder.InputContainerId);
        var outputContainer = _itemSlotSystem.GetItemOrNull(entity, SharedMaltGrinder.BeakerSlotId);

        if (inputContainer.ContainedEntities.Count <= 0 || outputContainer is null || !HasComp<FitsInDispenserComponent>(outputContainer.Value))
        {
            return;
        }

        var activeComp = AddComp<ActiveMaltGrinderComponent>(entity);
        activeComp.EndTime = _timing.CurTime + entity.Comp.WorkTime;
        _audioSystem.PlayPvs(entity.Comp.WorkSound, entity);
        _appearanceSystem.SetData(entity, MaltGrinderVisualLayers.On, true);
        UpdateUiState(entity);
    }

    private void OnEjectAll(Entity<MaltGrinderComponent> entity, ref MaltGrinderEjectChamberAllMessage args)
    {
        if (HasComp<ActiveMaltGrinderComponent>(entity))
            return;

        var inputContainer = _containerSystem.EnsureContainer<Container>(entity, SharedMaltGrinder.InputContainerId);

        if (inputContainer.ContainedEntities.Count <= 0)
            return;

        foreach (var item in inputContainer.ContainedEntities.ToList())
        {
            _containerSystem.Remove(item, inputContainer);
        }

        _audioSystem.PlayPvs(entity.Comp.ClickSound, entity);

        UpdateUiState(entity);
    }

    private void OnContainerRemovingAttempt(Entity<MaltGrinderComponent> entity, ref ContainerIsRemovingAttemptEvent args)
    {
        if (HasComp<ActiveMaltGrinderComponent>(entity))
            args.Cancel();
    }

    private void OnInteractUsing(Entity<MaltGrinderComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var heldEnt = args.Used;

        if (HasComp<ToolComponent>(heldEnt))
            return;

        if (HasComp<FitsInDispenserComponent>(heldEnt))
        {
            if (!_itemSlotSystem.TryGetSlot(entity.Owner, SharedMaltGrinder.BeakerSlotId, out var slot) || HasComp<ActiveMaltGrinderComponent>(entity))
                return;

            if (!_itemSlotSystem.TryInsertFromHand(entity.Owner, slot, args.User))
                return;

            args.Handled = true;
            UpdateUiState(entity);
            return;
        }

        var inputContainer = _containerSystem.EnsureContainer<Container>(entity.Owner, SharedMaltGrinder.InputContainerId);

        if (inputContainer.ContainedEntities.Count >= entity.Comp.StorageMaxEntities)
            return;

        if (!_whitelistSystem.IsValid(entity.Comp.InputWhitelist, heldEnt))
            return;

        if (!_containerSystem.Insert(heldEnt, inputContainer))
            return;

        args.Handled = true;
    }

    private void OnContainerModified(EntityUid uid, MaltGrinderComponent component, ContainerModifiedMessage args)
    {
        UpdateUiState(uid);
    }

    private void UpdateUiState(EntityUid uid)
    {
        if (!TryComp(uid, out MaltGrinderComponent? maltGrinder))
            return;

        var inputContainer = _containerSystem.EnsureContainer<Container>(uid, SharedMaltGrinder.InputContainerId);
        var outputContainer = _itemSlotSystem.GetItemOrNull(uid, SharedMaltGrinder.BeakerSlotId);
        var isProcessing = HasComp<ActiveMaltGrinderComponent>(uid);

        var state = new MaltGrinderInterfaceState(
            isProcessing: isProcessing,
            hasBeaker: outputContainer.HasValue,
            chamberfull: inputContainer.ContainedEntities.Count >= maltGrinder.StorageMaxEntities,
            chamberCount: inputContainer.ContainedEntities.Count
        );

        _userInterfaceSystem.SetUiState(uid, MaltGrinderUiKey.Key, state);
    }

    private void ProceedWork(EntityUid uid, MaltGrinderComponent maltGrinder)
    {
        var inputContainer = _containerSystem.EnsureContainer<Container>(uid, SharedMaltGrinder.InputContainerId);
        var outputContainer = _itemSlotSystem.GetItemOrNull(uid, SharedMaltGrinder.BeakerSlotId);

        if (outputContainer is null
            || !_solutionContainerSystem.TryGetFitsInDispenser(outputContainer.Value, out var containerSoln, out var containerSolution))
        {
            RemCompDeferred<ActiveMaltGrinderComponent>(uid);
            _appearanceSystem.SetData(uid, MaltGrinderVisualLayers.On, false);
            return;
        }

        foreach (var item in inputContainer.ContainedEntities.ToList())
        {
            var maltSolution = new Solution();
            maltSolution.AddReagent("Malt", FixedPoint2.New(5));

            if (maltSolution.Volume > containerSolution.AvailableVolume)
                continue;

            _solutionContainerSystem.TryAddSolution(containerSoln.Value, maltSolution);
            _containerSystem.Remove(item, inputContainer);
            Del(item);
        }

        RemCompDeferred<ActiveMaltGrinderComponent>(uid);
        _appearanceSystem.SetData(uid, MaltGrinderVisualLayers.On, false);
        UpdateUiState(uid);
    }
}
