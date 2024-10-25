// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.SS220.CultYogg.MiGo;

namespace Content.Server.SS220.CultYogg.MiGo;

public sealed class CultYoggHealSystem : SharedCultYoggHealSystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggHealComponent, ComponentStartup>(SetupMiGoHeal);
    }
    private void SetupMiGoHeal(Entity<CultYoggHealComponent> uid, ref ComponentStartup args)
    {
        uid.Comp.NextIncidentTime = uid.Comp.TimeBetweenIncidents;
    }
    public override void Update(float frameTime)//ToDo rewrite as Timespan
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CultYoggHealComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out var healComp, out var _))
        {
            healComp.NextIncidentTime -= frameTime;

            if (healComp.NextIncidentTime > 0)
                continue;

            Heal(uid, healComp);

            healComp.NextIncidentTime += healComp.TimeBetweenIncidents;
        }
    }
    public void Heal(EntityUid uid, CultYoggHealComponent component)
    {
        if (!TryComp<MobStateComponent>(uid, out var mobComp))
            return;

        if (!TryComp<DamageableComponent>(uid, out var damageableComp))
            return;

        _damageable.TryChangeDamage(uid, component.Heal, true, interruptsDoAfters: false, damageableComp);

        if (!_mobState.IsDead(uid, mobComp))
            return;

        if (_mobThreshold.TryGetDeadThreshold(uid, out var threshold) && damageableComp.TotalDamage < threshold)
        {
            _mobState.ChangeMobState(uid, MobState.Critical);
            _popup.PopupEntity(Loc.GetString("cult-yogg-resurrected-by-heal", ("target", uid)), uid, PopupType.Medium);

            if (!_mind.TryGetMind(uid, out var _, out var mind))
                return;

            if (mind.Session != null && mind.CurrentEntity != uid)
                _euiManager.OpenEui(new ReturnToBodyEui(mind, _mind), mind.Session);
        }
    }

}
