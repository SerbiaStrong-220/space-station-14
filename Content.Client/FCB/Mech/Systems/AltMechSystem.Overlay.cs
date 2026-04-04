// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.Damage;
using Content.Shared.FCB.Mech.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Player;

namespace Content.Client.FCB.Mech;

/// <inheritdoc/>
public sealed partial class AltMechSystem
{
    private void OnPlayerAttach(Entity<AltMechComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        DamageOverlayInit(args.Entity);
    }

    private void DamageOverlayInit(EntityUid entity)
    {
        ClearOverlay();

        if (!EntityManager.TryGetComponent<AltMechComponent>(entity, out var mechComp) || mechComp.PilotSlot.ContainedEntity == null)
            return;

        if (!EntityManager.TryGetComponent<MobStateComponent>(mechComp.PilotSlot.ContainedEntity, out var mobState))
            return;

        _overlay.AddOverlay(_damageOverlay);

        if (mobState.CurrentState != MobState.Dead)
            UpdateOverlays(entity, mobState);
    }

    private void OnPlayerDetached(Entity<AltMechComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _overlay.RemoveOverlay(_damageOverlay);
        ClearOverlay();
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.Target != _playerManager.LocalEntity)
            return;

        UpdateOverlays(args.Target, args.Component);
    }

    private void OnThresholdCheck(Entity<AltMechPilotComponent> ent, ref MobThresholdChecked args)
    {

        if (!(EntityManager.TryGetComponent(args.Target, out AltMechPilotComponent? pilot)))
            return;

        if (pilot.Mech != _playerManager.LocalEntity)
            return;

        UpdateOverlays(pilot.Mech, args.MobState, (DamageableComponent?) args.Damageable, args.Threshold);
    }

    private void ClearOverlay()
    {
        _damageOverlay.DeadLevel = 0f;
        _damageOverlay.CritLevel = 0f;
        _damageOverlay.PainLevel = 0f;
        _damageOverlay.OxygenLevel = 0f;
    }

    private void UpdateOverlays(EntityUid entity, MobStateComponent? mobState, DamageableComponent? damageable = null, MobThresholdsComponent? thresholds = null)
    {
        if (!EntityManager.TryGetComponent<AltMechComponent>(entity, out var mechComp) || mechComp.PilotSlot.ContainedEntity == null)
            return;

        var pilot = mechComp.PilotSlot.ContainedEntity;

        if (mobState == null && !EntityManager.TryGetComponent(pilot, out mobState) ||
            thresholds == null && !EntityManager.TryGetComponent(pilot, out thresholds) ||
            damageable == null && !EntityManager.TryGetComponent(pilot, out damageable))
            return;

        if (!thresholds.ShowOverlays)
        {
            ClearOverlay();
            return; //this entity intentionally has no overlays
        }

        if (!_mobThresholdSystem.TryGetIncapThreshold((EntityUid)pilot, out var foundThreshold, thresholds))
            return;

        var critThreshold = foundThreshold.Value;
        _damageOverlay.State = mobState.CurrentState;

        switch (mobState.CurrentState)
        {
            case MobState.Alive:
                {
                    FixedPoint2 painLevel = 0;
                    _damageOverlay.PainLevel = 0;

                    //if (!_statusEffects.TryEffectsWithComp<PainNumbnessStatusEffectComponent>(pilot, out _)) uncomment on upstream
                    if(!TryComp<PainNumbnessComponent>(pilot, out var numbnessComp))
                    {
                        foreach (var painDamageType in damageable.PainDamageGroups)
                        {
                            damageable.DamagePerGroup.TryGetValue(painDamageType, out var painDamage);
                            painLevel += painDamage;
                        }
                        _damageOverlay.PainLevel = FixedPoint2.Min(1f, painLevel / critThreshold).Float();

                        if (_damageOverlay.PainLevel < 0.05f) // Don't show damage overlay if they're near enough to max.
                        {
                            _damageOverlay.PainLevel = 0;
                        }
                    }

                    if (damageable.DamagePerGroup.TryGetValue("Airloss", out var oxyDamage))
                    {
                        _damageOverlay.OxygenLevel = FixedPoint2.Min(1f, oxyDamage / critThreshold).Float();
                    }

                    _damageOverlay.CritLevel = 0;
                    _damageOverlay.DeadLevel = 0;
                    break;
                }
            case MobState.Critical:
                {
                    if (!_mobThresholdSystem.TryGetDeadPercentage((EntityUid)pilot,
                            FixedPoint2.Max(0.0, damageable.TotalDamage), out var critLevel))
                        return;
                    _damageOverlay.CritLevel = critLevel.Value.Float();

                    _damageOverlay.PainLevel = 0;
                    _damageOverlay.DeadLevel = 0;
                    break;
                }
            case MobState.Dead:
                {
                    _damageOverlay.PainLevel = 0;
                    _damageOverlay.CritLevel = 0;
                    break;
                }
        }
    }
}
