// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.SS220.CultYogg.MiGo;
using Content.Server.SS220.GameTicking.Rules;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.SS220.CultYogg.MiGo;

public sealed partial class MiGoReplacementSystem : SharedMiGoSystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize() //ToDo check for stupidity
    {
        SubscribeLocalEvent<MiGoComponent, MobStateChangedEvent>(OnMobState);
        SubscribeLocalEvent<MiGoComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<MiGoComponent, PlayerDetachedEvent>(OnPlayerDetached);
    }
    private void OnPlayerAttached(Entity<MiGoComponent> ent, ref PlayerAttachedEvent args)
    {
        CheckTimerConditions(ent, ent.Comp);
    }
    private void OnPlayerDetached(Entity<MiGoComponent> ent, ref PlayerDetachedEvent args)
    {
        CheckTimerConditions(ent, ent.Comp);
    }

    ///<summary>
    /// On death removes active comps and gives genetic damage to prevent cloning, reduce this to allow cloning.
    ///</summary>
    private void OnMobState(Entity<MiGoComponent> ent, ref MobStateChangedEvent args)
    {
        CheckTimerConditions(ent, ent.Comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MiGoComponent>();
        while (query.MoveNext(out var uid, out var migoComp))
        {
            //if timer is on then count it
            if (!migoComp.ShouldBeCounted)
                continue;

            if (migoComp.ReplacementEventTime == null)
                continue;

            if (_timing.CurTime < migoComp.ReplacementEventTime)
                continue;

            var meta = MetaData(uid);

            var ev = new CultYoggAnouncementEvent(uid, Loc.GetString("cult-yogg-migo-can-replace", ("name", meta.EntityName)));
            RaiseLocalEvent(uid, ref ev, true);
        }
    }

    //Timer tweaking
    private void StartTimer(MiGoComponent comp)
    {
        if (comp.ReplacementEventTime != null)
            return;

        comp.ReplacementEventTime = _timing.CurTime + comp.BeforeReplacementCooldown;
    }

    //Delete replacement marker from a MiGo
    private void RemoveReplacement(EntityUid uid, MiGoComponent сomp)
    {
        сomp.ReplacementEventTime = null;

        var meta = MetaData(uid);

        var ev = new CultYoggAnouncementEvent(uid, Loc.GetString("cult-yogg-migo-cancel-replace", ("name", meta.EntityName)));
        RaiseLocalEvent(uid, ref ev, true);
    }

    private void CheckTimerConditions(EntityUid uid, MiGoComponent comp)
    {
        if (_mobState.IsDead(uid)) //if you are dead = timer
            StartTimer(comp);

        if (!TryComp<MindContainerComponent>(uid, out var mindComp) || (mindComp == null))
            return;

        if (mindComp.Mind == null) // if you ghosted = timer
            StartTimer(comp);

        if (!_mind.TryGetMind(uid, out var _, out var userMidComp) || (userMidComp == null))
            return;

        if (userMidComp.Session == null) // if you left = timer
            StartTimer(comp);

        RemoveReplacement(uid, comp);
    }
}
