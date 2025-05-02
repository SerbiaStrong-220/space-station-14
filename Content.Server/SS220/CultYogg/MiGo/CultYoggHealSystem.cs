// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Body.Systems;
using Content.Server.Body.Components;
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
using Robust.Shared.Timing;
using Robust.Server.Player;

namespace Content.Server.SS220.CultYogg.MiGo;

public sealed class CultYoggHealSystem : SharedCultYoggHealSystem
{

    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggHealComponent, ComponentStartup>(SetupMiGoHeal);
    }
    private void SetupMiGoHeal(Entity<CultYoggHealComponent> uid, ref ComponentStartup args)
    {
        uid.Comp.NextIncidentTime = _time.CurTime + uid.Comp.TimeBetweenIncidents;
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CultYoggHealComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out var healComp, out var _))
        {
            if (healComp.NextIncidentTime > _time.CurTime)
                continue;

            Heal(uid, healComp);

            healComp.NextIncidentTime = _time.CurTime + healComp.TimeBetweenIncidents;
        }
    }
    public void Heal(EntityUid uid, CultYoggHealComponent component)
    {
        if (!TryComp<MobStateComponent>(uid, out var mobComp))
            return;

        if (!TryComp<DamageableComponent>(uid, out var damageableComp))
            return;

        _damageable.TryChangeDamage(uid, component.Heal, true, interruptsDoAfters: false, damageableComp);

        _bloodstreamSystem.TryModifyBleedAmount(uid, component.BloodlossModifier);
        _bloodstreamSystem.TryModifyBloodLevel(uid, component.ModifyBloodLevel);

        if (!_mobState.IsDead(uid, mobComp))
            return;

        if (_mobThreshold.TryGetDeadThreshold(uid, out var threshold) && damageableComp.TotalDamage < threshold)
        {
            _mobState.ChangeMobState(uid, MobState.Critical);
            _popup.PopupEntity(Loc.GetString("cult-yogg-resurrected-by-heal", ("target", uid)), uid, PopupType.Medium);

            if (_mind.TryGetMind(uid, out var _, out var mind) &&
                mind.CurrentEntity == uid &&
                _playerManager.TryGetSessionById(mind.UserId, out var session))
                _euiManager.OpenEui(new ReturnToBodyEui(mind, _mind, _playerManager), session);
        }
    }
}
