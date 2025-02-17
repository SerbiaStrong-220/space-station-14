// copypaste from defib

using Content.Server.Atmos.Rotting;
using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Server.Mind;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Player;

namespace Content.Server.Surgery.ActionSystems;

public sealed class SurgeryResuscitateSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly RottingSystem _rotting = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public void Resuscitate(EntityUid target, EntityUid? performer, DamageSpecifier heal)
    {
        if (performer.HasValue)
            Resuscitate(target, performer.Value, heal);
        else
            Resuscitate(target, heal);
    }

    public void Resuscitate(EntityUid target, EntityUid performer, DamageSpecifier heal)
    {
        if (!TryComp<MobStateComponent>(target, out var mob) ||
            !TryComp<MobThresholdsComponent>(target, out var thresholds))
            return;

        var targetSession = GetEntitySession(target);
        var performerSession = GetEntitySession(performer);

        if (performerSession == null)
        {
            Log.Debug($"Tried to make a resuscitate action with null performer session. performer - {ToPrettyString(performer)}, target - {ToPrettyString(target)}");
            return;
        }

        if (_rotting.IsRotten(target))
        {
            _popup.PopupCursor("surgery-resuscitate-rotten", performerSession);
        }
        else if (HasComp<UnrevivableComponent>(target))
        {
            _popup.PopupCursor("surgery-resuscitate-unrevivable", performerSession);
        }
        else
        {
            if (_mobState.IsDead(target, mob))
                _damageable.TryChangeDamage(target, heal, true, origin: performer);

            if (_mobThreshold.TryGetThresholdForState(target, MobState.Dead, out var threshold) &&
                TryComp<DamageableComponent>(target, out var damageableComponent) &&
                damageableComponent.TotalDamage < threshold)
            {
                _mobState.ChangeMobState(target, MobState.Critical, mob, performer);
            }

            if (_mind.TryGetMind(target, out _, out var mind) &&
                mind.Session is { } playerSession)
            {
                targetSession = playerSession;
                // notify them they're being revived.
                if (mind.CurrentEntity != target)
                {
                    _euiManager.OpenEui(new ReturnToBodyEui(mind, _mind), targetSession);
                }
            }
            else
            {
                _popup.PopupCursor("surgery-resuscitate-no-mind", performerSession);
            }
        }
    }

    public void Resuscitate(EntityUid target, DamageSpecifier heal)
    {
        if (!TryComp<MobStateComponent>(target, out var mob) ||
            !TryComp<MobThresholdsComponent>(target, out var thresholds))
            return;

        var targetSession = GetEntitySession(target);

        if (_rotting.IsRotten(target))
        {
            return;
        }
        else if (HasComp<UnrevivableComponent>(target))
        {
            return;
        }
        else
        {
            if (_mobState.IsDead(target, mob))
                _damageable.TryChangeDamage(target, heal, true);

            if (_mobThreshold.TryGetThresholdForState(target, MobState.Dead, out var threshold) &&
                TryComp<DamageableComponent>(target, out var damageableComponent) &&
                damageableComponent.TotalDamage < threshold)
            {
                _mobState.ChangeMobState(target, MobState.Critical, mob);
            }

            if (_mind.TryGetMind(target, out _, out var mind) &&
                mind.Session is { } playerSession)
            {
                targetSession = playerSession;
                // notify them they're being revived.
                if (mind.CurrentEntity != target)
                {
                    _euiManager.OpenEui(new ReturnToBodyEui(mind, _mind), targetSession);
                }
            }
            else
            {
                return;
            }
        }
    }

    private ICommonSession? GetEntitySession(EntityUid uid)
    {
        if (!_mind.TryGetMind(uid, out var _, out var mindComponent))
            return null;

        return mindComponent.Session;
    }

}
