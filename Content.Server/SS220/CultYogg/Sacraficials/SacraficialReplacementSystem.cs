// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.GameTicking.Rules;
using Content.Server.SS220.Objectives.Components;
using Content.Server.SS220.Objectives.Systems;
using Content.Shared.SS220.CultYogg.Sacraficials;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Content.Server.SS220.Bed.Cryostorage;
using Content.Shared.Interaction.Events;

namespace Content.Server.SS220.CultYogg.Sacraficials;

public sealed partial class SacraficialReplacementSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly CultYoggRuleSystem _cultRule = default!;

    //dictionary of sacraficials uids and time when they left body by gibbing/ghosting/leaving anything
    private Dictionary<EntityUid, TimeSpan> _replaceSacrSchedule = [];
    private Dictionary<EntityUid, TimeSpan> _announceSchedule = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggSacrificialComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CultYoggSacrificialComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<CultYoggSacrificialComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<CultYoggSacrificialComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<CultYoggSacrificialComponent, BeingCryoDeletedEvent>(OnCryoDeleted);
        SubscribeLocalEvent<CultYoggSacrificialComponent, SuicideEvent>(OnSuicide);
    }
    private void OnInit(Entity<CultYoggSacrificialComponent> ent, ref ComponentInit args)
    {
        var ev = new CultYoggUpdateSacrObjEvent();
        var query = EntityQueryEnumerator<CultYoggSummonConditionComponent>();
        while (query.MoveNext(out var uid, out var _))
        {
            RaiseLocalEvent(uid, ref ev);
        }

        _cultRule.SendCultAnounce(Loc.GetString("cult-yogg-sacraficial-was-picked", ("name", MetaData(ent).EntityName)));
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
        _replaceSacrSchedule.Remove(ent);

        if (_announceSchedule.ContainsKey(ent))//if the announcement was not sent
        {
            _announceSchedule.Remove(ent);
            return;
        }

        _cultRule.SendCultAnounce(Loc.GetString("cult-yogg-sacraficial-cant-be-replaced", ("name", MetaData(ent).EntityName)));
    }

    private void OnPlayerDetached(Entity<CultYoggSacrificialComponent> ent, ref PlayerDetachedEvent args)
    {
        _replaceSacrSchedule.TryAdd(ent, _timing.CurTime + ent.Comp.ReplacementCooldown);
        _announceSchedule.TryAdd(ent, _timing.CurTime + ent.Comp.AnnounceReplacementCooldown);
    }

    private void OnCryoDeleted(Entity<CultYoggSacrificialComponent> ent, ref BeingCryoDeletedEvent args)
    {
        var ev = new SacraficialReplacementEvent(ent);
        RaiseLocalEvent(ent, ref ev, true);
    }

    private void ReplacamantStatusAnnounce(EntityUid uid)
    {
        if (!TryComp<CultYoggSacrificialComponent>(uid, out var comp))
            return;

        var time = (comp.ReplacementCooldown.TotalSeconds - comp.AnnounceReplacementCooldown.TotalSeconds).ToString();
        _cultRule.SendCultAnounce(Loc.GetString("cult-yogg-sacraficial-will-be-replaced", ("name", MetaData(uid).EntityName), ("time", time)));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        foreach (var pair in _replaceSacrSchedule)
        {
            if (_timing.CurTime < pair.Value)
                continue;

            var ev = new SacraficialReplacementEvent(pair.Key);
            RaiseLocalEvent(pair.Key, ref ev, true);

            _replaceSacrSchedule.Remove(pair.Key);
        }

        foreach (var pair in _announceSchedule)//it is stupid, but idk how to make it 1 time event without second System :(
        {
            if (_timing.CurTime < pair.Value)
                continue;

            ReplacamantStatusAnnounce(pair.Key);

            _announceSchedule.Remove(pair.Key);
        }
    }

    private void OnSuicide(Entity<CultYoggSacrificialComponent> ent, ref SuicideEvent args)
    {
        _replaceSacrSchedule.TryAdd(ent, _timing.CurTime + ent.Comp.ReplacementCooldown + ent.Comp.SuicidePenaltyTime);
    }
}
