// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Body.Systems;
using Content.Server.Damage.Systems;
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
using Robust.Shared.Player;
using Robust.Shared.Timing;

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
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggHealComponent, ComponentStartup>(SetupMiGoHeal);
        SubscribeLocalEvent<CultYoggHealComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void SetupMiGoHeal(Entity<CultYoggHealComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.NextIncidentTime = _time.CurTime + ent.Comp.TimeBetweenIncidents;
    }

    private void OnDamageChanged(Entity<CultYoggHealComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        if(!ent.Comp.ShouldStopOnDamage)
            return;

        var delta = args.DamageDelta.GetTotal();
        if (delta < ent.Comp.CancelDamageTreshhold)
            return;

        RemCompDeferred<CultYoggHealComponent>(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CultYoggHealComponent, MobStateComponent>();
        while (query.MoveNext(out var ent, out var healComp, out _))
        {
            if (healComp.NextIncidentTime > _time.CurTime)
                continue;

            Heal((ent, healComp));

            healComp.NextIncidentTime = _time.CurTime + healComp.TimeBetweenIncidents;
        }
    }

    public void Heal(Entity<CultYoggHealComponent> ent)
    {
        if (!TryComp<MobStateComponent>(ent, out var mobComp))
            return;

        if (!TryComp<DamageableComponent>(ent, out var damageableComp))
            return;

        _damageable.TryChangeDamage(ent, ent.Comp.Heal, true, interruptsDoAfters: false, damageableComp);

        _bloodstreamSystem.TryModifyBleedAmount(ent.Owner, ent.Comp.BloodlossModifier);
        _bloodstreamSystem.TryModifyBloodLevel(ent.Owner, ent.Comp.ModifyBloodLevel);

        _stamina.TryTakeStamina(ent, ent.Comp.ModifyStamina);

        if (!_mobState.IsDead(ent, mobComp))
            return;

        if (!_mobThreshold.TryGetDeadThreshold(ent, out var threshold))
            return;

        if (damageableComp.TotalDamage > threshold)
            return;

        _mobState.ChangeMobState(ent, MobState.Critical);
        _popup.PopupEntity(Loc.GetString("cult-yogg-resurrected-by-heal", ("target", ent)), ent, PopupType.Medium);

        if (!_mind.TryGetMind(ent, out _, out var mind))
            return;

        if (!_player.TryGetSessionById(mind.UserId, out var playerSession))
            return;

        if (mind.CurrentEntity == ent)
            return;

        _euiManager.OpenEui(new ReturnToBodyEui(mind, _mind, _player), playerSession);
    }
}
