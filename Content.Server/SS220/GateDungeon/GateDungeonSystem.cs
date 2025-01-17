// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Popups;
using Content.Shared.Gateway;
using Content.Shared.Interaction;
using Content.Shared.Tag;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.SS220.GateDungeon;

/// <summary>
/// This handles creates a new map from the list and connects them with teleports.
/// To work correctly from the place where teleportation takes place, two entities with a GateDungeonComponent are required
/// one must have the StartDungeon tag, the other must have the EndToStationDungeon tag.
/// The created map requires two entities with GateDungeonComp with tag, one must have the MediumDungeon tag,
/// the other must have the EndDungeon tag
/// </summary>
public sealed class GateDungeonSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly LinkedEntitySystem _linked = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GateDungeonComponent, ComponentStartup>(OnCreateDungeon);
        SubscribeLocalEvent<GateDungeonComponent, InteractHandEvent>(OnInteract);
    }

    private void OnCreateDungeon(Entity<GateDungeonComponent> ent, ref ComponentStartup args)
    {
        if(!_tagSystem.HasTag(ent.Owner, "StartDungeon"))
            return;

        _appearance.SetData(ent.Owner, GatewayVisuals.Active, false); //should be turned off at the beginning

        if(ent.Comp.PathDungeon == null)
            return;

        var mapDungeon = _random.Pick(ent.Comp.PathDungeon);

        _map.CreateMap(out var mapId);
        _loader.TryLoad(mapId, mapDungeon, out _);
        _mapManager.SetMapPaused(mapId, true);

        ent.Comp.MapId = mapId;

        Timer.Spawn(ent.Comp.ChargingTime,() => ChargingDone(ent.Owner));

        var gates = EntityQueryEnumerator<GateDungeonComponent>();

        var entGates = new List<EntityUid>();

        while (gates.MoveNext(out var entDungeon, out _))
        {
            entGates.Add(entDungeon);
        }

        if(ent.Comp.GateStart == null ||
           ent.Comp.GateMedium == null ||
           ent.Comp.GateEndToStation == null ||
           ent.Comp.GateEnd == null)
            return;

        foreach (var gate in entGates)
        {
            if (_tagSystem.HasTag(gate, "StartDungeon"))
                ent.Comp.GateStart.Add(gate);

            if (_tagSystem.HasTag(gate, "MediumDungeon"))
                ent.Comp.GateMedium.Add(gate);

            if(_tagSystem.HasTag(gate, "EndDungeon"))
                ent.Comp.GateEnd.Add(gate);

            if(_tagSystem.HasTag(gate, "EndToStationDungeon"))
                ent.Comp.GateEndToStation.Add(gate);
        }
    }

    private void ChargingDone(EntityUid ent)
    {
        if (!TryComp<GateDungeonComponent>(ent, out var gateComp))
            return;

        _mapManager.SetMapPaused(gateComp.MapId, false);

        gateComp.IsCharging = false;

        var currentGateStart = PickRandom(gateComp.GateStart);
        if (currentGateStart == default)
            return;

        var currentGateMedium = PickRandom(gateComp.GateMedium);
        if (currentGateMedium == default)
            return;

        var currentGateEnd = PickRandom(gateComp.GateEnd);
        if (currentGateEnd == default)
            return;

        var currentGateEndToStation = PickRandom(gateComp.GateEndToStation);
        if(currentGateEndToStation == default)
            return;

        _appearance.SetData(ent, GatewayVisuals.Active, true);

        EnsureComp<PortalComponent>(currentGateStart, out var portalStartComp);
        EnsureComp<PortalComponent>(currentGateEnd, out var portalMediumComp);

        portalStartComp.CanTeleportToOtherMaps = true;
        portalMediumComp.CanTeleportToOtherMaps = true;

        EnsureComp<LinkedEntityComponent>(currentGateStart, out _);
        EnsureComp<LinkedEntityComponent>(currentGateEnd, out _);

        _linked.TryLink(currentGateStart, currentGateMedium);
        _linked.TryLink(currentGateEnd, currentGateEndToStation);
    }

    private void OnInteract(Entity<GateDungeonComponent> ent, ref InteractHandEvent args)
    {
        if(!_tagSystem.HasTag(ent.Owner, "StartDungeon"))
            return;

        _popup.PopupEntity(ent.Comp.IsCharging
                ? Loc.GetString("gate-dungeon-is-charging")
                : Loc.GetString("gate-dungeon-already-charged"),
            ent.Owner,
            args.User);
    }

    private T? PickRandom<T>(IReadOnlyList<T>? list)
    {
        if (list == null || list.Count == 0)
            return default;

        return _random.Pick(list);
    }
}
