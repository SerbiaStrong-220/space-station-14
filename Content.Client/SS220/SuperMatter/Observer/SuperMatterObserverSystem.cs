// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.Power.Components;
using Content.Shared.Mobs;
using Content.Shared.SS220.SuperMatter.Ui;
using System.Linq;

namespace Content.Client.SS220.SuperMatter.Observer;

public sealed class SuperMatterObserverSystem : EntitySystem
{
    // 120 like 2 minutes with update rate 1 sec
    public const int MAX_CACHED_AMOUNT = 120;
    private List<Entity<SuperMatterObserverComponent>> _observerComps = new();
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SuperMatterObserverComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SuperMatterObserverComponent, ComponentRemove>(OnComponentRemove);

        SubscribeNetworkEvent<SuperMatterStateUpdate>(OnCrystalUpdate);
    }

    private void OnCrystalUpdate(SuperMatterStateUpdate args)
    {
        // store values to simulate real working data observer&collectors manufacture
        foreach (var observerEnt in _observerComps)
        {
            var (observerUid, observerComp) = observerEnt;
            // still it will store without power, cause, you know... caching =)
            observerComp.Names[args.Id] = args.Name;
            observerComp.DelaminationStatuses[args.Id] = args.Delaminate;
            AddToCacheList(observerComp.Integrities[args.Id], args.Integrity);
            AddToCacheList(observerComp.Pressures[args.Id], args.Pressure);
            AddToCacheList(observerComp.Temperatures[args.Id], args.Temperature);
            AddToCacheList(observerComp.Matters[args.Id], args.Matter);
            AddToCacheList(observerComp.InternalEnergy[args.Id], args.InternalEnergy);
            // check if power is On on console
            if (!(TryComp<ApcPowerReceiverComponent>(observerUid, out var powerReceiver)
                && powerReceiver.Powered))
                continue;
            // Send updateStates for opened UIs and panels

        }
    }
    private void OnComponentInit(Entity<SuperMatterObserverComponent> entity, ref ComponentInit args)
    {
        // very aware of client crashes
        _observerComps.Add(entity);
        _observerComps = _observerComps.Distinct().ToList();
    }
    private void OnComponentRemove(Entity<SuperMatterObserverComponent> entity, ref ComponentRemove args)
    {
        // very aware of client crashes
        _observerComps = _observerComps.Distinct().ToList();
        _observerComps.Remove(entity);
    }
    private void AddToCacheList<T>(List<T> listToAdd, T value)
    {
        if (listToAdd.Count == MAX_CACHED_AMOUNT)
            listToAdd.RemoveAt(0);
        listToAdd.Add(value);
    }
}
