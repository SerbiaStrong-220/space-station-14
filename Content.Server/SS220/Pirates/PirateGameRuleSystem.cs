using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Server.Spawners.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.SS220.Pirates;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Pirates;

public sealed partial class PirateGameRuleSystem : GameRuleSystem<PirateGameRuleComponent>
{
    private static readonly EntProtoId CaptainMindRole = "MindRolePirateCaptainExpansion";
    private static readonly EntProtoId CaptainSpawner = "SpawnPointPirateCaptainExpansion";
    private static readonly EntProtoId CaptainSpawnPoint = "SpawnPointPirateCaptain";
    private static readonly EntProtoId CrewSpawnPoint = "SpawnPointPirateCrew";

    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private SharedRoleSystem _roles = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PirateGameRuleComponent, RuleLoadedGridsEvent>(OnRuleLoadedGrids);
        SubscribeLocalEvent<PirateGameRuleComponent, AntagSelectLocationEvent>(OnSelectLocation,
            after: [typeof(RuleGridsSystem)]);
        SubscribeLocalEvent<PirateBaseComponent, GridSplitEvent>(OnPirateGridSplit);
    }

    private void OnRuleLoadedGrids(Entity<PirateGameRuleComponent> _, ref RuleLoadedGridsEvent args)
    {
        foreach (var grid in args.Grids)
            EnsureComp<PirateBaseComponent>(grid);
    }

    private void OnPirateGridSplit(Entity<PirateBaseComponent> _, ref GridSplitEvent args)
    {
        foreach (var newGrid in args.NewGrids)
            EnsureComp<PirateBaseComponent>(newGrid);
    }

    private void OnSelectLocation(Entity<PirateGameRuleComponent> rule, ref AntagSelectLocationEvent args)
    {
        if (!TryComp<RuleGridsComponent>(rule, out var ruleGrids) || ruleGrids.Map is not { } map)
            return;

        var spawnPoint = IsCaptain(rule, args) ? CaptainSpawnPoint : CrewSpawnPoint;
        var coordinates = new List<MapCoordinates>();
        var query = EntityQueryEnumerator<SpawnPointComponent, MetaDataComponent, TransformComponent>();

        while (query.MoveNext(out _, out _, out var metadata, out var transform))
        {
            if (metadata.EntityPrototype?.ID != spawnPoint.Id ||
                transform.MapID != map ||
                transform.GridUid is not { } grid ||
                !HasComp<PirateBaseComponent>(grid))
            {
                continue;
            }

            coordinates.Add(_transform.GetMapCoordinates(transform));
        }

        if (coordinates.Count == 0)
            return;

        args.Coordinates.Clear();
        args.Coordinates.AddRange(coordinates);
    }

    private bool IsCaptain(Entity<PirateGameRuleComponent> rule, AntagSelectLocationEvent args)
    {
        if (MetaData(args.Entity).EntityPrototype?.ID == CaptainSpawner.Id)
            return true;

        if (args.Session is not { } session || !TryComp<AntagSelectionComponent>(rule, out var selection))
            return false;

        return _antag.IsSelectedForMindRole(selection, session, CaptainMindRole);
    }

    protected override void AppendRoundEndText(EntityUid uid, PirateGameRuleComponent component,
        GameRuleComponent gameRule, ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);
        args.AddLine(Loc.GetString("pirate-round-end-title"));
        args.AddLine(Loc.GetString("pirate-round-end-summary",
            ("items", component.TotalItemsSold),
            ("loot", component.TotalLootValue)));
        args.AddLine(Loc.GetString("pirate-round-end-list-start"));

        var minds = EntityQueryEnumerator<MindComponent>();
        while (minds.MoveNext(out var mindUid, out var mind))
        {
            if (!_roles.MindHasRole<PirateCrewRoleComponent>(mindUid))
                continue;

            if (mind.OriginalOwnerUserId is not { } userId ||
                !_players.TryGetPlayerData(userId, out var sessionData))
            {
                continue;
            }

            var name = mind.CharacterName ?? Loc.GetString("pirate-round-end-unknown");
            args.AddLine(Loc.GetString("pirate-round-end-list-entry", ("name", name), ("user", sessionData.UserName)));
        }

        args.AddLine(string.Empty);
    }
}
