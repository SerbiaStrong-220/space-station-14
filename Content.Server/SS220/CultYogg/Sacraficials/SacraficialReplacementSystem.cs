// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.SS220.GameTicking.Rules;
using Content.Server.SS220.Objectives.Components;
using Content.Server.SS220.Objectives.Systems;
using Content.Shared.SS220.CultYogg.Sacraficials;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.SS220.CultYogg.Sacraficials;

public sealed partial class SacraficialReplacementSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    //dictionary of sacraficials uids and time when they left body by gibbing/ghosting/leaving anything
    private Dictionary<(EntityUid, NetUserId), TimeSpan> _replaceSacrSchedule = new();
    private Dictionary<(EntityUid, NetUserId), TimeSpan> _announceSchedule = new();

    //Count down the moment when sacraficial will be raplaced
    private TimeSpan _beforeReplacementCooldown = TimeSpan.FromSeconds(900);

    //Count down the moment when cultists will get an anounce about replacement
    private TimeSpan _announceReplacementCooldown = TimeSpan.FromSeconds(300);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggSacrificialComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CultYoggSacrificialComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<CultYoggSacrificialComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<CultYoggSacrificialComponent, PlayerDetachedEvent>(OnPlayerDetached);
    }
    private void OnInit(Entity<CultYoggSacrificialComponent> ent, ref ComponentInit args)
    {
        var ev = new CultYoggUpdateSacrObjEvent();
        var query = EntityQueryEnumerator<CultYoggSummonConditionComponent>();
        while (query.MoveNext(out var uid, out var _))
        {
            RaiseLocalEvent(uid, ref ev);
        }
    }
    private void OnRemove(Entity<CultYoggSacrificialComponent> ent, ref ComponentRemove args)
    {
        var ev = new CultYoggUpdateSacrObjEvent();
        var query = EntityQueryEnumerator<CultYoggSummonConditionComponent>();
        while (query.MoveNext(out var uid, out var _))
        {
            RaiseLocalEvent(uid, ref ev);
        }
    }
    private void OnPlayerAttached(Entity<CultYoggSacrificialComponent> ent, ref PlayerAttachedEvent args)
    {
        _replaceSacrSchedule.Remove((ent, args.Player.UserId));
        _announceSchedule.Remove((ent, args.Player.UserId));

        var meta = MetaData(ent);

        var ev = new CultYoggAnouncementEvent(ent, Loc.GetString("cult-yogg-sacraficial-cant-be-replaced", ("name", meta.EntityName)));
        RaiseLocalEvent(ent, ref ev, true);
    }

    private void OnPlayerDetached(Entity<CultYoggSacrificialComponent> ent, ref PlayerDetachedEvent args)
    {
        _replaceSacrSchedule.Add((ent, args.Player.UserId), _timing.CurTime);
        _announceSchedule.Add((ent, args.Player.UserId), _timing.CurTime);
    }

    private void ReplacamantStatusAnnounce(EntityUid uid)
    {
        if (!TryComp<CultYoggSacrificialComponent>(uid, out var comp))
            return;

        var meta = MetaData(uid);

        var ev = new CultYoggAnouncementEvent(uid, Loc.GetString("cult-yogg-sacraficial-may-be-replaced", ("name", meta.EntityName)));
        RaiseLocalEvent(uid, ref ev, true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        foreach (var pair in _replaceSacrSchedule)
        {
            if (_timing.CurTime < pair.Value + _beforeReplacementCooldown)
                continue;

            var ev = new SacraficialReplacementEvent(pair.Key.Item1, pair.Key.Item2);
            RaiseLocalEvent(pair.Key.Item1, ref ev, true);

            _replaceSacrSchedule.Remove(pair.Key);
        }

        foreach (var pair in _announceSchedule)//it is stupid, but idk how to make it 1 time event without second System :(
        {
            if (_timing.CurTime < pair.Value + _announceReplacementCooldown)
                continue;

            ReplacamantStatusAnnounce(pair.Key.Item1);

            _announceSchedule.Remove(pair.Key);
        }
    }
}
