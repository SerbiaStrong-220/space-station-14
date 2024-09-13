// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Antag;
using Content.Server.GameTicking.Rules;
using Content.Server.Respawn;
using Content.Server.SS220.Markers;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Map;

namespace Content.Server.SS220.SpiderQueen;

public sealed class SpiderQueenRuleSystem : GameRuleSystem<SpiderQueenRuleComponent>
{
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SpecialRespawnSystem _specialRespawn = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpiderQueenRuleComponent, AntagSelectLocationEvent>(OnAntagSelectLocation);
    }

    private void OnAntagSelectLocation(Entity<SpiderQueenRuleComponent> ent, ref AntagSelectLocationEvent args)
    {
        if (args.Handled)
            return;

        List<MapCoordinates> validCoordinates = new();
        var query = EntityQueryEnumerator<AntagSpawnMarkerComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (_entityWhitelist.IsWhitelistFail(ent.Comp.MarkersWhitelist, uid) ||
                !TryComp<TransformComponent>(uid, out var transform))
                continue;

            validCoordinates.Add(_transform.ToMapCoordinates(transform.Coordinates));
        }

        if (validCoordinates.Count > 0)
            args.Coordinates = validCoordinates;
        else
        {
            EntityUid? grid = null;
            EntityUid? map = null;
            foreach (var station in _station.GetStationsSet())
            {
                if (!TryComp<StationDataComponent>(station, out var data))
                    continue;

                grid = _station.GetLargestGrid(data);
                if (!grid.HasValue)
                    continue;

                map = Transform(grid.Value).MapUid;
                if (!map.HasValue)
                    continue;

                break;
            }

            if (grid.HasValue && map.HasValue &&
                _specialRespawn.TryFindRandomTile(grid.Value, map.Value, 30, out var randomCoords))
                args.Coordinates.Add(_transform.ToMapCoordinates(randomCoords));
        }
    }
}

