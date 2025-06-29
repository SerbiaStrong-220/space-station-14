// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Administration.Managers;
using Content.Server.Antag;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Server.Respawn;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Mind;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Server.Player;

namespace Content.Server.SS220.DarkReaper;

public sealed class DarkReaperMajorRuleSystem : GameRuleSystem<DarkReaperMajorRuleComponent>
{
    [Dependency] private readonly SpecialRespawnSystem _specialRespawn = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private readonly ISawmill _sawmill = Logger.GetSawmill("DarkReaperMajorRule");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DarkReaperMajorRuleComponent, AntagSelectEntityEvent>(OnAntagSelectEntity);
        SubscribeLocalEvent<DarkReaperMajorRuleComponent, AntagSelectLocationEvent>(OnAntagSelectLocation);
    }

    protected override void AppendRoundEndText(EntityUid uid, DarkReaperMajorRuleComponent component, GameRuleComponent gameRule, ref RoundEndTextAppendEvent ev)
    {
        var mindQuery = GetEntityQuery<MindComponent>();
        foreach (var reaperRule in EntityQuery<DarkReaperMajorRuleComponent>())
        {
            var mindId = reaperRule.ReaperMind;
            if (mindQuery.TryGetComponent(mindId, out var mind) && _playerManager.TryGetSessionById(mind.UserId, out var session))
            {
                ev.AddLine(Loc.GetString("darkreaper-roundend-user", ("user", session)));
            }
        }
    }

    private void OnAntagSelectEntity(Entity<DarkReaperMajorRuleComponent> ent, ref AntagSelectEntityEvent args)
    {
        if (args.Handled)
            return;

        args.Entity = Spawn(ent.Comp.RunePrototypeId);
    }

    private void OnAntagSelectLocation(Entity<DarkReaperMajorRuleComponent> ent, ref AntagSelectLocationEvent args)
    {
        if (args.Handled)
            return;

        // Try find primary grid of a station
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

        if (!grid.HasValue || !map.HasValue)
        {
            _chatManager.SendAdminAnnouncement(Loc.GetString("darkreaper-failed-spawn-grid"));
            return;
        }

        _specialRespawn.TryFindRandomTile(grid.Value, map.Value, 30, out var runeCoords);
        if (runeCoords == EntityCoordinates.Invalid)
            runeCoords = Transform(grid.Value).Coordinates;

        args.Coordinates.Add(_transform.ToMapCoordinates(runeCoords));
    }
}
