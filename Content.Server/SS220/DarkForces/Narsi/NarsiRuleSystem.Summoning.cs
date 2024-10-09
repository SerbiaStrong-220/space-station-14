using System.Linq;
using Content.Server.SS220.DarkForces.Narsi.Runes.Events;
using Content.Shared.Audio;

namespace Content.Server.SS220.DarkForces.Narsi;

public sealed partial class NarsiRuleSystem
{
    private void InitSummoning()
    {
        SubscribeLocalEvent<NarsiSummoningEndEvent>(OnNarsiSpawnedEvent);
        SubscribeLocalEvent<NarsiSummoningStartEvent>(OnNarsiSummoningStartEvent);
        SubscribeLocalEvent<NarsiSummoningCanceledEvent>(OnNarsiSummoningCanceledEvent);
    }

    private void OnNarsiSpawnedEvent(NarsiSummoningEndEvent args)
    {
        if (args.Cancelled)
            return;

        var cultistRule = EntityQuery<NarsiRuleComponent>().FirstOrDefault();
        if (cultistRule == null)
            return;

        var delay = TimeSpan.FromSeconds(180);

        cultistRule.RoundEndAt = _gameTiming.CurTime + delay;
        cultistRule.WinStateStatus = WinState.CultistWon;
        cultistRule.RuneSource = args.Source;

        //_soundSystem.StopStationEventMusic(args.Source, StationEventMusicType.Narsi);
    }

    private void OnNarsiSummoningStartEvent(NarsiSummoningStartEvent args)
    {
        if (args.Cancelled)
            return;

        var cultistRule = EntityQuery<NarsiRuleComponent>().FirstOrDefault();
        if (cultistRule == null)
            return;

        var stationUid = _station.GetOwningStation(args.Source);
        if (stationUid != null)
        {
            _alertLevel.SetLevel(stationUid.Value, "gamma", false, true, true, true);
        }

        _chatSystem.DispatchStationAnnouncement(
            args.Source,
            Loc.GetString("narsi-rule-cultist-start-summoning", ("pos", GetEntityPosString(args.Source))),
            "ИИ-Помощник",
            false,
            Color.Yellow
        );

        cultistRule.NarsiRepeatSoundAt = _gameTiming.CurTime + TimeSpan.FromSeconds(35f);
        cultistRule.WinStateStatus = WinState.NarsiSummoning;
        cultistRule.RuneSource = args.Source;

        //_soundSystem.StopStationEventMusic(args.Source, StationEventMusicType.Narsi);
        //_soundSystem.DispatchStationEventMusic(args.Source, cultistRule.NarsiSummonSound, StationEventMusicType.Narsi);
    }

    private void OnNarsiSummoningCanceledEvent(NarsiSummoningCanceledEvent args)
    {
        var cultistRule = EntityQuery<NarsiRuleComponent>().FirstOrDefault();
        if (cultistRule == null)
            return;

        cultistRule.WinStateStatus = WinState.Idle;
        cultistRule.RuneSource = args.Source;

        //_soundSystem.StopStationEventMusic(args.Source, StationEventMusicType.Narsi);
    }
}
