// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Linq;
using Content.Server.Popups;
using Content.Shared.Gateway;
using Content.Shared.Interaction;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.SS220.GateDungeon;

/// <summary>
/// This handles creates a new map from the list and connects them with teleports.
/// To work correctly from the place where teleportation takes place, two entities with a GateDungeonComponent are required
/// one must have the gateType: Start, the other must have the gateType: Mid.
/// The created map requires two entities with GateDungeonComp with tag, one must have the gateType: End,
/// the other must have the gateType: ToStation
/// </summary>

public sealed class GateDungeonSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly LinkedEntitySystem _linked = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;

    private Dictionary<GateType, List<EntityUid>> _gateList = new();

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GateDungeonComponent, MapInitEvent>(OnCreateDungeon);
        SubscribeLocalEvent<GateDungeonComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<GateDungeonComponent, ComponentShutdown>(OnDelete);
    }

    private void OnCreateDungeon(Entity<GateDungeonComponent> ent, ref MapInitEvent args)
    {
        if(ent.Comp.GateType != GateType.Start)
            return;

        _appearance.SetData(ent.Owner, GatewayVisuals.Active, false); //should be turned off at the beginning

        Timer.Spawn(ent.Comp.ChargingTime,() => ChargingDone(ent.Owner));

    }

    private void ChargingDone(EntityUid ent)
    {
        if (!TryComp<GateDungeonComponent>(ent, out var gateComp))
            return;

        CreateMap(gateComp);

        var currentGateStart = PickRandom(_gateList[GateType.Start]);
        var currentGateMedium = PickRandom(_gateList[GateType.Mid]);
        var currentGateEnd = PickRandom(_gateList[GateType.End]);
        var currentGateEndToStation = PickRandom(_gateList[GateType.ToStation]);

        if (currentGateStart == default ||
            currentGateMedium == default ||
            currentGateEnd == default ||
            currentGateEndToStation == default)
            return;

        _appearance.SetData(ent, GatewayVisuals.Active, true);

        gateComp.IsCharging = false;

        EnsureComp<PortalComponent>(currentGateStart, out var portalStartComp);
        EnsureComp<PortalComponent>(currentGateEnd, out var portalMediumComp);

        portalStartComp.CanTeleportToOtherMaps = true;
        portalMediumComp.CanTeleportToOtherMaps = true;

        _linked.TryLink(currentGateStart, currentGateMedium);
        _linked.TryLink(currentGateEnd, currentGateEndToStation);
    }

    private void OnInteract(Entity<GateDungeonComponent> ent, ref InteractHandEvent args)
    {
        if(ent.Comp.GateType != GateType.Start)
            return;

        _popup.PopupEntity(ent.Comp.IsCharging
                ? Loc.GetString("gate-dungeon-is-charging")
                : Loc.GetString("gate-dungeon-already-charged"),
            ent.Owner,
            args.User);
    }

    private void OnDelete(Entity<GateDungeonComponent> ent, ref ComponentShutdown args)
    {
        if (_gateList.TryGetValue(ent.Comp.GateType, out var gateList))
            gateList.Remove(ent.Owner);
    }

    private T? PickRandom<T>(IReadOnlyList<T>? list)
    {
        if (list == null || list.Count == 0)
            return default;

        return _random.Pick(list);
    }

    private void CreateMap(GateDungeonComponent comp)
    {

        if(comp.PathDungeon == null)
            return;

        var mapDungeon = _random.Pick(comp.PathDungeon);
        if (mapDungeon == null)
            return;

        var path = new ResPath(mapDungeon);
        _map.CreateMap(out var mapId);
        _loader.TryLoadGrid(mapId, path, out _);

        _meta.SetEntityName(_map.GetMapOrInvalid(mapId), "Gate dungeon"); //just a plug for the name

        var gates = EntityQueryEnumerator<GateDungeonComponent>();

        var entGates = new List<EntityUid>();

        while (gates.MoveNext(out var entDungeon, out _))
        {
            entGates.Add(entDungeon);
        }

        _gateList = Enum.GetValues(typeof(GateType))
            .Cast<GateType>()
            .ToDictionary(gateType => gateType, _ => new List<EntityUid>());


        foreach (var gate in entGates)
        {
            if(!TryComp<GateDungeonComponent>(gate, out var gateComp))
                continue;

            switch (gateComp.GateType)
            {
                case GateType.Start:
                    _gateList[GateType.Start].Add(gate);
                    break;

                case GateType.Mid:
                    _gateList[GateType.Mid].Add(gate);
                    break;

                case GateType.End:
                    _gateList[GateType.End].Add(gate);
                    break;

                case GateType.ToStation:
                    _gateList[GateType.ToStation].Add(gate);
                    break;

                default:
                    continue;
            }
        }
    }
}

public enum GateType : byte
{
    Start,
    Mid,
    End,
    ToStation
}
