using System.Linq;
using Content.Server.SS220.DarkForces.Narsi.Cultist.Roles.Narsi;
using Content.Server.SS220.DarkForces.Saint.Chaplain.Abilities;
using Content.Shared.Audio;

namespace Content.Server.SS220.DarkForces.Narsi;

public sealed partial class NarsiRuleSystem
{
    private void InitChaplain()
    {
        SubscribeLocalEvent<ChaplainNarsiExileEnableEvent>(OnChaplainAvailable);
        SubscribeLocalEvent<ChaplainNarsiExileStartEvent>(OnChaplainStartAbility);
        SubscribeLocalEvent<ChaplainNarsiExileFinishedEvent>(OnChaplainEndAbility);
        SubscribeLocalEvent<ChaplainNarsiExileCanceledEvent>(OnChaplainCancelAbility);
    }

    private void OnChaplainCancelAbility(ChaplainNarsiExileCanceledEvent args)
    {
        if (args.Cancelled)
            return;

        var cultistRule = EntityQuery<NarsiRuleComponent>().FirstOrDefault();
        if (cultistRule == null)
            return;

        SendNarsiMessage(args.Chaplain, Loc.GetString("narsi-rule-chaplain-lose"));
        cultistRule.WinStateStatus = WinState.CultistWon;

        //_soundSystem.StopStationEventMusic(args.Chaplain, StationEventMusicType.Narsi);
        _roundEndSystem.EndRound();
    }

    private void OnChaplainEndAbility(ChaplainNarsiExileFinishedEvent args)
    {
        if (args.Cancelled)
            return;

        SendNarsiMessage(args.Chaplain, Loc.GetString("narsi-rule-chaplain-win"));
        DeleteNarsi();

        _roundEndSystem.EndRound();
        //_soundSystem.StopStationEventMusic(args.Chaplain, StationEventMusicType.Narsi);
    }

    private void DeleteNarsi()
    {
        var query = EntityQueryEnumerator<NarsiComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            QueueDel(uid);
        }
    }

    private void OnChaplainStartAbility(ChaplainNarsiExileStartEvent args)
    {
        if (args.Cancelled)
            return;

        var cultistRule = EntityQuery<NarsiRuleComponent>().FirstOrDefault();
        if (cultistRule == null)
            return;

        SendNarsiMessage(args.Chaplain, Loc.GetString("narsi-rule-chaplain-start-exile", ("pos", GetEntityPosString(args.Chaplain))));
        cultistRule.WinStateStatus = WinState.NarsiLastStand;

        //_soundSystem.StopStationEventMusic(args.Chaplain, StationEventMusicType.Narsi);
        //_soundSystem.DispatchStationEventMusic(args.Chaplain, cultistRule.NarsiExileSound, StationEventMusicType.Narsi);
    }

    private string GetEntityPosString(EntityUid uid)
    {
        var pos = _transform.GetMapCoordinates(uid).Position;

        var x = (int)pos.X;
        var y = (int)pos.Y;

        return $"({x}, {y})";
    }

    private void OnChaplainAvailable(ChaplainNarsiExileEnableEvent args)
    {
        if (args.Cancelled)
            return;

        var cultistRule = EntityQuery<NarsiRuleComponent>().FirstOrDefault();
        if (cultistRule == null)
            return;

        var chaplainName = MetaData(args.Chaplain).EntityName;
        _chatSystem.DispatchStationAnnouncement(
            args.Chaplain,
            Loc.GetString("narsi-rule-chaplain-exile-available", ("chaplainName", chaplainName)),
            "ИИ-Помощник",
            true,
            Color.Yellow
        );
    }

    private void SendNarsiMessage(EntityUid source, string message)
    {
        _chatSystem.DispatchStationAnnouncement(
            source,
            message,
            "Нар'Си",
            true,
            Color.FromHex("#4D004B")
        );
    }
}
