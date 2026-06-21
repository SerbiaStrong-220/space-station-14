// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.SS220.Mech.Components;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Player;

namespace Content.Client.SS220.Mech;

/// <inheritdoc/>
public sealed partial class AltMechSystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    private void OnPlayerAttach(Entity<AltMechComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        DamageOverlayInit(args.Entity);
    }

    private void DamageOverlayInit(EntityUid entity)
    {
        ClearOverlay();

        if (!TryComp<AltMechComponent>(entity, out var mechComp) || mechComp.PilotSlot.ContainedEntity == null)
            return;

        if (!TryComp<MobStateComponent>(mechComp.PilotSlot.ContainedEntity, out var mobState))
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

        if (!TryComp(args.Target, out AltMechPilotComponent? pilot))
            return;

        if (pilot.Mech != _playerManager.LocalEntity)
            return;

        UpdateOverlays(pilot.Mech, args.MobState, (DamageableComponent?)args.Damageable, args.Threshold);
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
        if (!TryComp<AltMechComponent>(entity, out var mechComp) || mechComp.PilotSlot.ContainedEntity == null)
            return;

        var pilot = mechComp.PilotSlot.ContainedEntity;

        if (pilot is not { Valid: true } pilotValidated)
            return;

        if (mobState == null && !TryComp(pilotValidated, out mobState) ||
            thresholds == null && !TryComp(pilotValidated, out thresholds) ||
            damageable == null && !TryComp(pilotValidated, out damageable))
            return;

        if (!thresholds.ShowOverlays)
        {
            ClearOverlay();
            return; //this entity intentionally has no overlays
        }

        if (!_mobThresholdSystem.TryGetIncapThreshold(pilotValidated, out var foundThreshold, thresholds))
            return;

        var damagePerGroup = _damageableSystem.GetDamagePerGroup((pilotValidated, damageable));
        var critThreshold = foundThreshold.Value;
        _damageOverlay.State = mobState.CurrentState;

        switch (mobState.CurrentState)
        {
            case MobState.Alive:
                {
                    FixedPoint2 painLevel = 0;
                    _damageOverlay.PainLevel = 0;


                    if (!_statusEffects.TryEffectsWithComp<PainNumbnessStatusEffectComponent>(entity, out _))
                    {
                        foreach (var painDamageType in damageable.PainDamageGroups)
                        {

                            damagePerGroup.TryGetValue(painDamageType, out var painDamage);
                            painLevel += painDamage;
                        }
                        _damageOverlay.PainLevel = FixedPoint2.Min(1f, painLevel / critThreshold).Float();

                        if (_damageOverlay.PainLevel < 0.05f) // Don't show damage overlay if they're near enough to max.
                        {
                            _damageOverlay.PainLevel = 0;
                        }
                    }

                    if (damagePerGroup.TryGetValue("Airloss", out var oxyDamage))
                    {
                        _damageOverlay.OxygenLevel = FixedPoint2.Min(1f, oxyDamage / critThreshold).Float();
                    }

                    _damageOverlay.CritLevel = 0;
                    _damageOverlay.DeadLevel = 0;
                    break;
                }
            case MobState.Critical:
                {
                    if (!_mobThresholdSystem.TryGetDeadPercentage(pilotValidated,
                            FixedPoint2.Max(0.0, _damageableSystem.GetTotalDamage(pilotValidated)),
                            out var critLevel))
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
