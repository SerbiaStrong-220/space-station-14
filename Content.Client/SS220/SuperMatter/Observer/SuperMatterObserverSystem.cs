// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.Power.Components;
using Robust.Client.GameObjects;
using Content.Shared.SS220.SuperMatter.Ui;
using Content.Client.SS220.SuperMatter.Ui;
using Robust.Shared.Timing;

namespace Content.Client.SS220.SuperMatter.Observer;

public sealed class SuperMatterObserverSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    // 120 like 2 minutes with update rate 1 sec
    public const int MAX_CACHED_AMOUNT = 120;
    private const float UpdateDelay = 1f;
    private HashSet<Entity<SuperMatterObserverComponent>> _observerEntities = new();
    private List<EntityUid> _smReceiverUIOwnersToInit = new();
    private TimeSpan _nextUpdateTime = default!;
    private HashSet<Entity<SuperMatterObserverReceiverComponent>> _receivers = new();
    private SortedList<int, EntityUid> _processedReceivers = new();
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SuperMatterObserverReceiverComponent, BoundUIOpenedEvent>(OnReceiverBoundUIOpened);
        SubscribeLocalEvent<SuperMatterObserverReceiverComponent, BoundUIClosedEvent>(OnReceiverBoundUIClosed);
        SubscribeNetworkEvent<SuperMatterStateUpdate>(OnCrystalUpdate);
    }
    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_gameTiming.CurTime > _nextUpdateTime)
        {
            // kinda simulate servers and clients working
            _nextUpdateTime += TimeSpan.FromSeconds(UpdateDelay);
            foreach (var smReceiverOwner in new List<EntityUid>(_smReceiverUIOwnersToInit))
            {
                if (!HasComp<TransformComponent>(smReceiverOwner)
                    || Transform(smReceiverOwner).GridUid == null)
                    continue;

                _entityLookup.GetChildEntities(Transform(smReceiverOwner).GridUid!.Value, _observerEntities);
                foreach (var (observerUid, _) in _observerEntities)
                {
                    if (TryComp<ApcPowerReceiverComponent>(observerUid, out var powerReceiver)
                        && powerReceiver.Powered)
                    {
                        if (!_userInterface.HasUi(smReceiverOwner, SuperMatterObserverUiKey.Key))
                            return;
                        if (TrySendToUIState(smReceiverOwner,
                                                new SuperMatterObserverInitState(new List<Entity<SuperMatterObserverComponent>>(_observerEntities))))
                            _smReceiverUIOwnersToInit.Remove(smReceiverOwner);
                        break;
                    }
                }
                _observerEntities.Clear();
            }
        }
    }

    private void OnCrystalUpdate(SuperMatterStateUpdate args)
    {
        // store values to simulate real working data observer&collectors manufacture
        if (args.SMGridId == null)
            return;
        _entityLookup.GetChildEntities(EntityManager.GetEntity(args.SMGridId.Value), _observerEntities);
        foreach (var observerEnt in _observerEntities)
        {
            // To make possible many SMs on different grids
            var (observerUid, observerComp) = observerEnt;
            if (!HasComp<TransformComponent>(observerUid))
                continue;
            if (Transform(observerUid).GridUid != EntityManager.GetEntity(args.SMGridId))
                continue;
            // still it will store without power, cause, you know... caching =)
            observerComp.Names[args.Id] = args.Name;
            observerComp.DelaminationStatuses[args.Id] = args.Delaminate;
            if (!observerComp.Integrities.ContainsKey(args.Id))
            {
                observerComp.Integrities[args.Id] = new();
                observerComp.Pressures[args.Id] = new();
                observerComp.Temperatures[args.Id] = new();
                observerComp.Matters[args.Id] = new();
                observerComp.InternalEnergy[args.Id] = new();
            }
            AddToCacheList(observerComp.Integrities[args.Id], args.Integrity);
            AddToCacheList(observerComp.Pressures[args.Id], args.Pressure);
            AddToCacheList(observerComp.Temperatures[args.Id], args.Temperature);
            AddToCacheList(observerComp.Matters[args.Id], args.Matter);
            AddToCacheList(observerComp.InternalEnergy[args.Id], args.InternalEnergy);
            // here dispatches events to sprites of SM itself
            // RaiseLocalEvent(GetIntegrityState changed then change sprite)
            // check if console has power
            if (!(TryComp<ApcPowerReceiverComponent>(observerUid, out var powerReceiver)
                && powerReceiver.Powered))
                continue;
            // Send updateStates for opened UIs and panels

            // think of the same way as with _observerEntities, but will have problems if SMObserver will be on other maps etc
            _entityLookup.GetEntitiesOnMap(Transform(observerUid).MapID, _receivers);
            // logic
            foreach (var receiver in _receivers)
            {
                if (_processedReceivers.ContainsKey(receiver.Owner.Id))
                    continue;
                if (TrySendToUIState(receiver.Owner, new SuperMatterObserverUpdateState(args.Id, args.Name, args.Integrity, args.Pressure,
                                                                                      args.Temperature, args.Matter, args.InternalEnergy, args.Delaminate)))
                    _processedReceivers.Add(receiver.Owner.Id, receiver.Owner);
            }
            _receivers.Clear();
            // RaiseLocalEvent(SMpanels -> accept information)
        }
        _observerEntities.Clear();
        _processedReceivers.Clear();
    }
    private void OnReceiverBoundUIOpened(Entity<SuperMatterObserverReceiverComponent> entity, ref BoundUIOpenedEvent args)
    {
        if (!_userInterface.HasUi(entity, args.UiKey))
            return;
        _smReceiverUIOwnersToInit.Add(entity.Owner);

    }
    private void OnReceiverBoundUIClosed(Entity<SuperMatterObserverReceiverComponent> entity, ref BoundUIClosedEvent args)
    {
        if (!_userInterface.HasUi(entity, args.UiKey))
            return;
        // Lest hope it wont duplicate
        _smReceiverUIOwnersToInit.Remove(entity.Owner);
    }
    private void AddToCacheList<T>(List<T> listToAdd, T value)
    {
        if (listToAdd.Count == MAX_CACHED_AMOUNT)
            listToAdd.RemoveAt(0);
        listToAdd.Add(value);
    }
    private bool TrySendToUIState(EntityUid uid, BoundUserInterfaceState state)
    {
        if (!_userInterface.TryGetOpenUi(uid, SuperMatterObserverUiKey.Key, out var bui))
        {
            return false;
        }
        ((SuperMatterObserverBUI)bui).DirectUpdateState(state);
        return true;
    }
}
