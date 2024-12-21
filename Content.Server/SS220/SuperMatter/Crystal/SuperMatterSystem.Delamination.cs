// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.AlertLevel;
using Content.Server.Explosion.EntitySystems;
using Content.Server.SS220.SuperMatterCrystal.Components;
using Content.Server.Station.Systems;
using Content.Server.Tesla.Components;
using Content.Server.Tesla.EntitySystems;
using Content.Shared.Singularity.Components;
using Content.Shared.SS220.SuperMatter.Functions;

namespace Content.Server.SS220.SuperMatterCrystal;

public sealed partial class SuperMatterSystem : EntitySystem
{
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly AlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly TeslaEnergyBallSystem _teslaEnergyBall = default!;

    private const float SECONDS_BEFORE_EXPLOSION = 13f;
    private const float IntegrityRegenerationStep = 5f;
    private const float IntegrityRegenerationEnd = 40f;

    public void MarkAsLaminated(Entity<SuperMatterComponent> crystal, float? secondsToBlow = null)
    {
        var (crystalUid, comp) = crystal;
        var secondsToExplosion = secondsToBlow.HasValue ? secondsToBlow.Value : SECONDS_BEFORE_EXPLOSION;

        comp.TimeOfDelamination = _gameTiming.CurTime + TimeSpan.FromSeconds(secondsToExplosion);
        comp.AccumulatedRegenerationDelamination = 0f;
        comp.NextRegenerationThreshold = IntegrityRegenerationStep;
        comp.IsDelaminate = true;

        var ev = new SuperMatterDelaminateStarted(comp.TimeOfDelamination);
        RaiseLocalEvent(crystalUid, ev);
        SendAdminChatAlert(crystal, Loc.GetString("supermatter-admin-alert-delamination-start", ("time", secondsToExplosion)));
        _ambientSound.SetSound(crystalUid, comp.DelamSound);

        TryChangeStationAlertLevel(crystal, comp.DelaminateAlertLevel, out comp.PreviousAlertLevel);
    }
    public void StopDelamination(Entity<SuperMatterComponent> crystal)
    {
        var (crystalUid, comp) = crystal;

        comp.IsDelaminate = false;
        comp.Integrity = 20f;
        comp.AccumulatedRegenerationDelamination = 0f;
        comp.NextRegenerationThreshold = IntegrityRegenerationStep;

        var ev = new SuperMatterDelaminateStopped();
        RaiseLocalEvent(crystalUid, ev);
        SendAdminChatAlert(crystal, Loc.GetString("supermatter-admin-alert-delamination-stop"));
        _ambientSound.SetSound(crystalUid, comp.CalmSound);

        if (comp.PreviousAlertLevel != null)
        {
            TryChangeStationAlertLevel(crystal, comp.PreviousAlertLevel, out _);
            comp.PreviousAlertLevel = null;
        }
        // the order of announce and alert level is important
        StationAnnounceIntegrity(crystal, AnnounceIntegrityTypeEnum.DelaminationStopped);
    }

    private void UpdateDelamination(Entity<SuperMatterComponent> crystal)
    {
        if (!crystal.Comp.IsDelaminate)
            return;

        if (crystal.Comp.IntegrityDamageAccumulator < 0)
            crystal.Comp.AccumulatedRegenerationDelamination -= crystal.Comp.IntegrityDamageAccumulator;

        if (crystal.Comp.AccumulatedRegenerationDelamination > crystal.Comp.NextRegenerationThreshold)
        {
            crystal.Comp.TimeOfDelamination += TimeSpan.FromSeconds(1f);
            crystal.Comp.NextRegenerationThreshold += IntegrityRegenerationStep;

            var ev = new SuperMatterDelaminateTimeChanged(crystal.Comp.TimeOfDelamination);
            RaiseLocalEvent(crystal, ev);
        }
        if (crystal.Comp.AccumulatedRegenerationDelamination > IntegrityRegenerationEnd)
            StopDelamination(crystal);
        if (_gameTiming.CurTime > crystal.Comp.TimeOfDelamination)
        {
            Delaminate(crystal);
            return;
        }
        if (_gameTiming.CurTime > crystal.Comp.NextDamageStationAnnouncement)
        {
            crystal.Comp.NextDamageStationAnnouncement += TimeSpan.FromSeconds(IntegrityDamageStationAnnouncementDelay);
            StationAnnounceIntegrity(crystal, AnnounceIntegrityTypeEnum.Delamination);
        }
    }
    private void Delaminate(Entity<SuperMatterComponent> crystal)
    {
        var smState = SuperMatterFunctions.GetSuperMatterPhase(crystal.Comp.Temperature,
                                                crystal.Comp.PressureAccumulator / crystal.Comp.UpdatesBetweenBroadcast);
        SendAdminChatAlert(crystal, Loc.GetString("supermatter-admin-alert-delamination-end", ("state", smState)));
        EntityUid? spawnedUid = null;
        switch (smState)
        {
            case SuperMatterPhaseState.ResonanceRegion:
                spawnedUid = Spawn(crystal.Comp.ResonanceSpawnPrototype, Transform(crystal.Owner).Coordinates);
                break;
            case SuperMatterPhaseState.SingularityRegion:
                spawnedUid = Spawn(crystal.Comp.SingularitySpawnPrototype, Transform(crystal.Owner).Coordinates);
                break;
            case SuperMatterPhaseState.TeslaRegion:
                spawnedUid = Spawn(crystal.Comp.TeslaSpawnPrototype, Transform(crystal.Owner).Coordinates);
                break;
            default:
                _explosion.TriggerExplosive(crystal.Owner);
                break;
        }

        if (spawnedUid.HasValue
            && TryComp<TeslaEnergyBallComponent>(spawnedUid.Value, out var teslaComp))
            _teslaEnergyBall.AdjustEnergy(spawnedUid.Value, teslaComp, 1000f);

        TryChangeStationAlertLevel(crystal, crystal.Comp.CrystalDestroyAlertLevel, out _);
        StationAnnounceIntegrity(crystal, AnnounceIntegrityTypeEnum.Explosion, smState);
    }
}
