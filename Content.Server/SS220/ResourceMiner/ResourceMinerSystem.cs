// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using System.Linq;
using Content.Server.Audio;
using Content.Server.Materials;
using Content.Shared.Materials;
using Content.Shared.Materials.OreSilo;
using Content.Shared.SS220.ResourceMiner;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.SS220.ResourceMiner;

public sealed class ResourceMinerSystem : EntitySystem
{
    [Dependency] private readonly AmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly PointLightSystem _pointLight = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<ResourceMinerComponent>(ResourceMinerSettings.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnBUIOpen);
            subs.Event<RequestAvailableSilos>(OnRequestAvailableSilos);
            subs.Event<SetResourceMinerSilo>(OnSetResourceMinerSilo);
        });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var silos = EntityQueryEnumerator<ResourceMinerComponent>();

        while (silos.MoveNext(out var uid, out var resourceMinerComponent))
        {
            if (resourceMinerComponent.Silo is null)
                continue;

            if (_gameTiming.CurTime < resourceMinerComponent.NextUpdate)
                continue;

            resourceMinerComponent.NextUpdate = _gameTiming.CurTime + resourceMinerComponent.TimeBetweenUpdate;
            AddResourcesToStorage((uid, resourceMinerComponent));
        }
    }

    private void AddResourcesToStorage(Entity<ResourceMinerComponent> entity)
    {
        if (entity.Comp.Silo is not { } materialStorage)
        {
            Log.Error($"Called {nameof(AddResourcesToStorage)} with of entity {ToPrettyString(entity)} with null silo uid provided!");
            return;
        }

        _materialStorage.TryChangeMaterialAmount(materialStorage, entity.Comp.GenerationAmount);
    }

    private void OnBUIOpen(Entity<ResourceMinerComponent> entity, ref BoundUIOpenedEvent msg)
    {
        SendAvailableSilos(entity);
    }

    private void OnRequestAvailableSilos(Entity<ResourceMinerComponent> entity, ref RequestAvailableSilos msg)
    {
        SendAvailableSilos(entity);
    }

    private void OnSetResourceMinerSilo(Entity<ResourceMinerComponent> entity, ref SetResourceMinerSilo msg)
    {
        if (!ValidateSilo(GetEntity(msg.Silo), entity))
            return;

        if (!TryGetEntity(msg.Silo, out var netSilo))
            return;

        entity.Comp.Silo = netSilo;

        _pointLight.SetColor(entity, entity.Comp.WorkingColor);
        _ambientSound.SetSound(entity, entity.Comp.WorkSound);

        Dirty(entity);
    }

    private void SendAvailableSilos(Entity<ResourceMinerComponent> entity)
    {
        var silos = new HashSet<Entity<MaterialStorageComponent, OreSiloComponent>>();
        _entityLookup.GetEntitiesOnMap(_transformSystem.GetMapId(entity.Owner), silos);

        _userInterface.SetUiState(entity.Owner, ResourceMinerSettings.Key, new AvailableSilosMiner([.. silos.Where(x => ValidateSilo(x.Owner, entity))
                                                                                                            .Select(x => GetNetEntity(x.Owner))]));
    }

    private bool ValidateSilo(EntityUid siloUid, Entity<ResourceMinerComponent> minerEntity)
    {
        if (!HasComp<MaterialStorageComponent>(siloUid))
            return false;

        if (!HasComp<OreSiloComponent>(siloUid))
            return false;

        return _transformSystem.GetMap(siloUid) == _transformSystem.GetMap(minerEntity.Owner);
    }
}
