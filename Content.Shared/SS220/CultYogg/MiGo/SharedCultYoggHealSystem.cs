// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Lidgren.Network;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.CultYogg.MiGo;

public abstract class SharedCultYoggHealSystem : EntitySystem
{
    [Dependency] private readonly SharedBloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggHealComponent, ComponentStartup>(SetupMiGoHeal);
        SubscribeLocalEvent<CultYoggHealComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void SetupMiGoHeal(Entity<CultYoggHealComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.NextHealingTickTime = _time.CurTime + ent.Comp.TimeBetweenHealingTicks;
    }

    private void OnDamageChanged(Entity<CultYoggHealComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        if (!ent.Comp.ShouldStopOnDamage)
            return;

        var delta = args.DamageDelta.GetTotal();
        if (delta < ent.Comp.CancelDamageTreshhold)
            return;

        RemCompDeferred<CultYoggHealComponent>(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CultYoggHealComponent>();
        while (query.MoveNext(out var ent, out var healComp))
        {
            if (healComp.HealingEffectTime is { } endTime && endTime <= _time.CurTime)
            {
                RemCompDeferred<CultYoggHealComponent>(ent);
                continue;
            }

            if (healComp.NextHealingTickTime > _time.CurTime)
                continue;

            Heal((ent, healComp));

            healComp.NextHealingTickTime = _time.CurTime + healComp.TimeBetweenHealingTicks;
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

        SendReturnToBodyEui(ent);
    }

    protected virtual void SendReturnToBodyEui(EntityUid ent) { }
}
