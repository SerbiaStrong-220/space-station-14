using Content.Server.SS220.DarkForces.Narsi.Progress;
using Content.Server.AlertLevel;
using Content.Server.Audio;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Shared.Audio;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Timing;
using Content.Server.Antag;

namespace Content.Server.SS220.DarkForces.Narsi;

public sealed partial class NarsiRuleSystem : GameRuleSystem<NarsiRuleComponent>
{
    [Dependency] private readonly AlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    //[Dependency] private readonly ServerGlobalSoundSystem _soundSystem = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly NarsiCultProgressSystem _progressSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NarsiRuleComponent, AntagSelectionEnd>(OnAntagSelectionEnd);

        InitChaplain();
        InitSummoning();
    }

    private void OnAntagSelectionEnd(EntityUid uid, NarsiRuleComponent component, ref AntagSelectionEnd args)
    {
        _progressSystem.FindNewCultistLeader();
    }

    protected override void Started(EntityUid uid, NarsiRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        _progressSystem.CreateProgress();
    }

    protected override void ActiveTick(EntityUid uid, NarsiRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (component.WinStateStatus == WinState.CultistWon && _gameTiming.CurTime > component.RoundEndAt)
        {
            _roundEndSystem.EndRound();
            component.WinStateStatus = WinState.Idle;
        }

        if (component.WinStateStatus != WinState.NarsiSummoning || _gameTiming.CurTime <= component.NarsiRepeatSoundAt)
            return;

        component.NarsiRepeatSoundAt = _gameTiming.CurTime + TimeSpan.FromSeconds(35f);

        //_soundSystem.StopStationEventMusic(component.RuneSource, StationEventMusicType.Narsi);
        //_soundSystem.DispatchStationEventMusic(component.RuneSource, component.NarsiSummonSound,
        //    StationEventMusicType.Narsi);
    }

    protected override void AppendRoundEndText(EntityUid uid,
        NarsiRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        var result = GetGameModeResultLine(component);
        args.AddLine(result);
    }

    private string GetGameModeResultLine(NarsiRuleComponent narsiRule)
    {
        var message = narsiRule.WinStateStatus switch
        {
            WinState.CultistWon => "narsi-rule-win-state-cultist-won",
            WinState.NarsiLastStand => "narsi-rule-win-state-last-stand",
            WinState.NarsiSummoning => "narsi-rule-win-state-summoning",
            _ => "narsi-rule-win-state-idle"
        };

        return Loc.GetString(message);
    }
}
