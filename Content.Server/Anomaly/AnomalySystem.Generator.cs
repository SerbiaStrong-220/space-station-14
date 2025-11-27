using System.Linq;
using System.Numerics;
using Content.Server.Anomaly.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Anomaly;
using Content.Shared.CCVar;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.Emag.Systems;
using Content.Shared.Materials;
using Content.Shared.Radio;
using Robust.Shared.Audio;
using Content.Shared.Physics;
using Content.Shared.Pinpointer;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Content.Shared.Power;
using Content.Shared.SS220.Anomaly;
using Robust.Shared.Map;

namespace Content.Server.Anomaly;

/// <summary>
/// This handles anomalous vessel as well as
/// the calculations for how many points they
/// should produce.
/// </summary>
public sealed partial class AnomalySystem
{
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    [Dependency] private readonly EmagSystem _emag = default!;

    private void InitializeGenerator()
    {
        SubscribeLocalEvent<AnomalyGeneratorComponent, BoundUIOpenedEvent>(OnGeneratorBUIOpened);
        SubscribeLocalEvent<AnomalyGeneratorComponent, MaterialAmountChangedEvent>(OnGeneratorMaterialAmountChanged);
        SubscribeLocalEvent<AnomalyGeneratorComponent, AnomalyGeneratorGenerateButtonPressedEvent>(OnGenerateButtonPressed);
        SubscribeLocalEvent<AnomalyGeneratorComponent, PowerChangedEvent>(OnGeneratorPowerChanged);
        SubscribeLocalEvent<GeneratingAnomalyGeneratorComponent, ComponentStartup>(OnGeneratingStartup);

        //ss220 add anomaly place start
        SubscribeLocalEvent<AnomalyGeneratorComponent, GotEmaggedEvent>(OnGotEmagged);
        SubscribeLocalEvent<AnomalyGeneratorComponent, AnomalyGeneratorChooseAnomalyPlaceMessage>(OnChoosePlaceMessage);
        //ss220 add anomaly place end
    }

    private void OnGeneratorPowerChanged(EntityUid uid, AnomalyGeneratorComponent component, ref PowerChangedEvent args)
    {
        _ambient.SetAmbience(uid, args.Powered);
    }

    private void OnGeneratorBUIOpened(EntityUid uid, AnomalyGeneratorComponent component, BoundUIOpenedEvent args)
    {

        UpdateGeneratorUi(uid, component, args.Actor);  //ss220 add anomaly place
    }

    private void OnGeneratorMaterialAmountChanged(EntityUid uid, AnomalyGeneratorComponent component, ref MaterialAmountChangedEvent args)
    {
        UpdateGeneratorUi(uid, component);
    }

    private void OnGenerateButtonPressed(EntityUid uid, AnomalyGeneratorComponent component, AnomalyGeneratorGenerateButtonPressedEvent args)
    {
        TryGeneratorCreateAnomaly(uid, component);
    }

    public void UpdateGeneratorUi(EntityUid uid, AnomalyGeneratorComponent component, EntityUid? user = null)  //ss220 add anomaly place start
    {
        var materialAmount = _material.GetMaterialAmount(uid, component.RequiredMaterial);

        var state = new AnomalyGeneratorUserInterfaceState(component.CooldownEndTime, materialAmount, component.MaterialPerAnomaly);
        _ui.SetUiState(uid, AnomalyGeneratorUiKey.Key, state);

        //ss220 add anomaly place start
        if (component.EmaggedUser != null && user != null && component.EmaggedUser.Value == user.Value)
        {
            List<AnomalyGeneratorEmagStruct> beacons = [];

            var beaconsQuery = EntityQueryEnumerator<NavMapBeaconComponent>();

            while (beaconsQuery.MoveNext(out var beacon, out var comp))
            {
                if (_station.GetOwningStation(beacon) != _station.GetOwningStation(uid))
                    continue;

                var name = comp.DefaultText is null ? MetaData(beacon).EntityName : Loc.GetString(comp.DefaultText);

                var beaconStruct = new AnomalyGeneratorEmagStruct
                {
                    Beacon = GetNetEntity(beacon),
                    Name = name,
                };

                beacons.Add(beaconStruct);
            }

            var emaggedMessage = new AnomalyGeneratorEmaggedEventMessage(beacons);
            _ui.ServerSendUiMessage(uid, AnomalyGeneratorUiKey.Key, emaggedMessage, user.Value);
        }
        //ss220 add anomaly place end
    }

    public void TryGeneratorCreateAnomaly(EntityUid uid, AnomalyGeneratorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!this.IsPowered(uid, EntityManager))
            return;

        if (Timing.CurTime < component.CooldownEndTime)
            return;

        if (!_material.TryChangeMaterialAmount(uid, component.RequiredMaterial, -component.MaterialPerAnomaly))
            return;

        var generating = EnsureComp<GeneratingAnomalyGeneratorComponent>(uid);
        generating.EndTime = Timing.CurTime + component.GenerationLength;
        generating.AudioStream = Audio.PlayPvs(component.GeneratingSound, uid, AudioParams.Default.WithLoop(true))?.Entity;
        component.CooldownEndTime = Timing.CurTime + component.CooldownLength;
        UpdateGeneratorUi(uid, component);
    }

    public void SpawnOnRandomGridLocation(EntityUid grid, string toSpawn)
    {
        if (!TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        var xform = Transform(grid);

        var targetCoords = xform.Coordinates;
        var gridBounds = gridComp.LocalAABB.Scale(_configuration.GetCVar(CCVars.AnomalyGenerationGridBoundsScale));

        for (var i = 0; i < 25; i++)
        {
            //ss220 add anomaly place start
            var randomX = Random.Next((int)gridBounds.Left, (int)gridBounds.Right);
            var randomY = Random.Next((int)gridBounds.Bottom, (int)gridBounds.Top);

            var tile = new Vector2i(randomX, randomY);

            if (TryFindValidSpawn(grid, gridComp, tile, out var coords))
            {
                Spawn(toSpawn, coords);
                return;
            }
            //ss220 add anomaly place end
        }

        Spawn(toSpawn, targetCoords);
    }

    //ss220 add anomaly place start
    public void SpawnNearBeacon(EntityUid beaconUid, string toSpawn, MinMax dist)
    {
        var beaconXform = Transform(beaconUid);
        var pos = beaconXform.Coordinates;

        var gridUid = beaconXform.GridUid;
        if (gridUid is null || !TryComp<MapGridComponent>(gridUid.Value, out var grid))
            return;

        for (var i = 0; i < 25; i++)
        {
            var point = (pos.Position + Random.NextAngle().ToVec() * dist.Next(Random)).Floored();
            var tile = new Vector2i(point.X, point.Y);

            if (TryFindValidSpawn(gridUid.Value, grid, tile, out var coords))
            {
                Spawn(toSpawn, coords);
                return;
            }
        }

        SpawnOnRandomGridLocation(gridUid.Value, toSpawn);
    }

    private bool TryFindValidSpawn(EntityUid grid, MapGridComponent gridComp, Vector2i tile, out EntityCoordinates target)
    {
        target = default;

        var xform = Transform(grid);

        // no air-blocked areas.
        if (_atmosphere.IsTileSpace(grid, gridComp.Owner, tile) ||
            _atmosphere.IsTileAirBlocked(grid, tile, mapGridComp: gridComp))
        {
            return false;
        }

        // don't spawn inside of solid objects
        var physQuery = GetEntityQuery<PhysicsComponent>();
        foreach (var ent in _mapSystem.GetAnchoredEntities(grid, gridComp, tile))
        {
            if (!physQuery.TryGetComponent(ent, out var body))
                continue;

            if (body.BodyType == BodyType.Static &&
                body.Hard &&
                (body.CollisionLayer & (int)CollisionGroup.Impassable) != 0)
            {
                return false;
            }
        }

        var localPos = _mapSystem.GridTileToLocal(grid, gridComp, tile);
        var mapPos = _transform.ToMapCoordinates(localPos);

        // AntiAnomaly zones
        var query = AllEntityQuery<AntiAnomalyZoneComponent, TransformComponent>();
        while (query.MoveNext(out _, out var zone, out var antiXform))
        {
            if (antiXform.MapID != mapPos.MapId)
                continue;

            var zonePos = _transform.GetWorldPosition(antiXform);
            if ((zonePos - mapPos.Position).LengthSquared() < zone.ZoneRadius * zone.ZoneRadius)
                return false;
        }

        target = localPos;
        return true;
    }
    //ss220 add anomaly place end

    private void OnGeneratingStartup(EntityUid uid, GeneratingAnomalyGeneratorComponent component, ComponentStartup args)
    {
        Appearance.SetData(uid, AnomalyGeneratorVisuals.Generating, true);
    }

    //ss220 add anomaly place start
    private void OnGotEmagged(Entity<AnomalyGeneratorComponent> ent, ref GotEmaggedEvent args)
    {
        if (args.Type != EmagType.Interaction)
            return;

        ent.Comp.EmaggedUser = args.UserUid;
        args.Handled = true;
    }

    private void OnChoosePlaceMessage(Entity<AnomalyGeneratorComponent> ent, ref AnomalyGeneratorChooseAnomalyPlaceMessage args)
    {
        var beacon = GetEntity(args.Beacon);

        if (TerminatingOrDeleted(beacon))
            return;

        ent.Comp.ChosenBeacon = beacon;
        TryGeneratorCreateAnomaly(ent.Owner, ent.Comp);
    }
    //ss220 add anomaly place end

    private void OnGeneratingFinished(EntityUid uid, AnomalyGeneratorComponent component)
    {
        var xform = Transform(uid);

        if (_station.GetStationInMap(xform.MapID) is not { } station ||
            _station.GetLargestGrid(station) is not { } grid)
        {
            if (xform.GridUid == null)
                return;
            grid = xform.GridUid.Value;
        }

        //ss220 add anomaly place start
        if (component is { EmaggedUser: not null, ChosenBeacon: not null })
            SpawnNearBeacon(component.ChosenBeacon.Value, component.SpawnerPrototype, component.EmaggedDistance);
        else
            SpawnOnRandomGridLocation(grid, component.SpawnerPrototype);

        component.EmaggedUser = null;
        //ss220 add anomaly place end
        RemComp<GeneratingAnomalyGeneratorComponent>(uid);
        Appearance.SetData(uid, AnomalyGeneratorVisuals.Generating, false);
        Audio.PlayPvs(component.GeneratingFinishedSound, uid);

        var message = Loc.GetString("anomaly-generator-announcement");
        _radio.SendRadioMessage(uid, message, _prototype.Index<RadioChannelPrototype>(component.ScienceChannel), uid);
    }

    private void UpdateGenerator()
    {
        var query = EntityQueryEnumerator<GeneratingAnomalyGeneratorComponent, AnomalyGeneratorComponent>();
        while (query.MoveNext(out var ent, out var active, out var gen))
        {
            if (Timing.CurTime < active.EndTime)
                continue;

            active.AudioStream = _audio.Stop(active.AudioStream);
            OnGeneratingFinished(ent, gen);
        }
    }
}
