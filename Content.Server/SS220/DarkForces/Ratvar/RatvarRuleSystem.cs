using System.Linq;
using Content.Server.SS220.DarkForces.Ratvar.Righteous.Progress;
using Content.Server.SS220.DarkForces.Ratvar.Righteous.Progress.Events;
using Content.Server.AlertLevel;
using Content.Server.Antag;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking.Rules;
using Content.Server.RoundEnd;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Timing;
using Content.Server.Station.Systems;

namespace Content.Server.SS220.DarkForces.Ratvar;

public sealed class RatvarRuleSystem : GameRuleSystem<RatvarRuleComponent>
{
    [Dependency] private readonly RatvarProgressSystem _progressSystem = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly AlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RatvarSpawnStartedEvent>(OnRatvarSpawnStartedEvent);
        SubscribeLocalEvent<RatvarSpawnCanceledEvent>(OnRatvarSpawnCancelEvent);
        SubscribeLocalEvent<RatvarSpawnedEvent>(OnRatvarSpawnedEvent);

        SubscribeLocalEvent<RatvarRuleComponent, AfterAntagEntitySelectedEvent>(OnRighteousSelected);
    }

    private void OnRighteousSelected(EntityUid uid, RatvarRuleComponent component, ref AfterAntagEntitySelectedEvent args)
    {
        _progressSystem.SetupRighteous(args.EntityUid);
    }

    protected override void Started(EntityUid uid, RatvarRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        _progressSystem.CreateProgress();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<RatvarRuleComponent>();
        while (query.MoveNext(out _, out var component))
        {
            if (component.WinState != WinState.RighteousWon || component.ForceRoundEnd > time)
                continue;

            _roundEndSystem.CancelRoundEndCountdown();
            _roundEndSystem.EndRound();
        }
    }

    private void OnRatvarSpawnedEvent(ref RatvarSpawnedEvent ev)
    {
        var rule = EntityQuery<RatvarRuleComponent>().FirstOrDefault();
        if (rule == null)
            return;

        rule.WinState = WinState.RighteousWon;

        _chatSystem.DispatchStationAnnouncement(
            ev.Ratvar,
            Loc.GetString("ratvar-spawn-end"),
            Loc.GetString("ratvar-name"),
            false,
            Color.FromHex("#b87333")
        );
        _roundEndSystem.RequestRoundEnd(checkCooldown: false);
        rule.ForceRoundEnd = _timing.CurTime + TimeSpan.FromMinutes(5);
    }

    private void OnRatvarSpawnCancelEvent(ref RatvarSpawnCanceledEvent ev)
    {
        var rule = EntityQuery<RatvarRuleComponent>().FirstOrDefault();
        if (rule == null)
            return;

        rule.WinState = WinState.Idle;
    }

    private void OnRatvarSpawnStartedEvent(ref RatvarSpawnStartedEvent ev)
    {
        var rule = EntityQuery<RatvarRuleComponent>().FirstOrDefault();
        if (rule == null)
            return;

        var position = _transform.GetMapCoordinates(ev.Portal).Position;
        var stationUid = _station.GetOwningStation(ev.Portal);
        if (stationUid != null)
        {
            _alertLevel.SetLevel(stationUid.Value, "gamma", true, true, true);
        }

        _chatSystem.DispatchStationAnnouncement(
            ev.Portal,
            Loc.GetString("ratvar-spawn-start", ("position", position)),
            Loc.GetString("station-helper-name"),
            false,
            Color.Yellow
        );

        rule.WinState = WinState.Summoning;
    }
}
