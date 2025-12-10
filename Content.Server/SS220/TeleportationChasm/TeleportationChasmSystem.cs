// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Light.Components;
using Content.Shared.SS220.TeleportationChasm;
using Content.Shared.Station;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.SS220.TeleportationChasm;

public sealed class TeleportationChasmSystem : SharedTeleportationChasmSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;
    [Dependency] private readonly AnchorableSystem _anchorable = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)//we cant teleport in shared, cause wierd shit happened
    {
        base.Update(frameTime);

        List<EntityUid> toRemove = [];

        var query = EntityQueryEnumerator<TeleportationChasmFallingComponent>();
        while (query.MoveNext(out var uid, out var chasm))
        {
            if (_timing.CurTime < chasm.NextDeletionTime)
                continue;

            TeleportToRandomLocation(uid);

            toRemove.Add(uid);
        }

        foreach (var uid in toRemove)
        {
            RemCompDeferred<TeleportationChasmFallingComponent>(uid);
            _blocker.UpdateCanMove(uid);
            DirtyEntity(uid);
        }
    }

    private void TeleportToRandomLocation(EntityUid ent)
    {
        if (_station.GetStations().FirstOrNull() is not { } station) // only "proper" way to find THE station
            return;

        var validLocations = new List<EntityCoordinates>();

        var locations = EntityQueryEnumerator<PoweredLightComponent, TransformComponent>();
        while (locations.MoveNext(out var uid, out _, out var transform))
        {
            var owningStation = _station.GetOwningStation(uid);//rude, but working

            if (owningStation != station)
                continue;

            validLocations.Add(transform.Coordinates);
        }

        if (TryTeleportFromCoordList(validLocations, ent))
            _adminLog.Add(LogType.Teleport, $"{ent:targer} got teleported on random location on station");
    }

    private bool TryTeleportFromCoordList(List<EntityCoordinates> coords, EntityUid teleported)
    {
        if (coords.Count == 0)
        {
            Log.Warning($"I couldn't teleport the {teleported} because there were no locations left to teleport to");
            return false;
        }

        //I didnt found normal ways to check empty tiles
        var teleportLocation = _random.Pick(coords);

        var xform = Transform(teleported);
        _transformSystem.SetCoordinates(teleported, xform, teleportLocation);
        return true;
    }
}
