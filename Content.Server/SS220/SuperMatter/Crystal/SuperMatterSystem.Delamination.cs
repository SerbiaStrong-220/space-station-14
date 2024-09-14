// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.AlertLevel;
using Content.Server.Construction.Completions;
using Content.Server.Explosion.EntitySystems;
using Content.Server.SS220.SuperMatterCrystal.Components;
using Content.Server.Station.Systems;
using Content.Shared.SS220.SuperMatter.Functions;
using FastAccessors;

namespace Content.Server.SS220.SuperMatterCrystal;

public sealed partial class SuperMatterSystem : EntitySystem
{
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly AlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly StationSystem _station = default!;

    private const float SECONDS_BEFORE_EXPLOSION = 13f;
    private const float IntegrityRegenerationStep = 5f;
    private const float IntegrityRegenerationEnd = 40f;

    public void MarkAsLaminated(Entity<SuperMatterComponent> crystal, float? secondsToBlow = null)
    {
        crystal.Comp.TimeOfDelamination = _gameTiming.CurTime
                        + TimeSpan.FromSeconds(secondsToBlow.HasValue ? secondsToBlow.Value : SECONDS_BEFORE_EXPLOSION);
        crystal.Comp.AccumulatedRegenerationDelamination = 0f;
        crystal.Comp.NextRegenerationThreshold = IntegrityRegenerationStep;
        crystal.Comp.IsDelaminate = true;

        var stationUid = _station.GetStationInMap(Transform(crystal.Owner).MapID);
        if (!stationUid.HasValue)
            return;
        crystal.Comp.PreviousAlertLevel = _alertLevel.GetLevel(stationUid.Value);
        _alertLevel.SetLevel(stationUid.Value, "yellow", true, true, true, true);
    }
    public void StopDelamination(Entity<SuperMatterComponent> crystal)
    {
        crystal.Comp.IsDelaminate = false;
        crystal.Comp.Integrity = 20f;
        crystal.Comp.AccumulatedRegenerationDelamination = 0f;
        crystal.Comp.NextRegenerationThreshold = IntegrityRegenerationStep;

        var stationUid = _station.GetStationInMap(Transform(crystal.Owner).MapID);
        if (stationUid.HasValue && crystal.Comp.PreviousAlertLevel != null)
            _alertLevel.SetLevel(stationUid.Value, crystal.Comp.PreviousAlertLevel, true, true, true, true);
        StationAnnounceIntegrity(crystal, AnnounceIntegrityTypeEnum.DelaminationStopped);
    }

    private void UpdateDelamination(Entity<SuperMatterComponent> crystal)
    {
        if (!crystal.Comp.IsDelaminate)
            return;

        if (crystal.Comp.IntegrityDamageAccumulator < 0)
            crystal.Comp.AccumulatedRegenerationDelamination -= crystal.Comp.IntegrityDamageAccumulator;

        if (crystal.Comp.IntegrityDamageAccumulator > crystal.Comp.NextRegenerationThreshold)
        {
            crystal.Comp.TimeOfDelamination += TimeSpan.FromSeconds(1f);
            crystal.Comp.NextRegenerationThreshold += IntegrityRegenerationStep;
        }
        if (crystal.Comp.IntegrityDamageAccumulator > IntegrityRegenerationEnd)
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
        switch (smState)
        {
            case SuperMatterPhaseState.ResonanceRegion:
                Spawn(crystal.Comp.ResonanceSpawnPrototype, Transform(crystal.Owner).Coordinates);
                break;
            case SuperMatterPhaseState.SingularityRegion:
                Spawn(crystal.Comp.SingularitySpawnPrototype, Transform(crystal.Owner).Coordinates);
                break;
            case SuperMatterPhaseState.TeslaRegion:
                Spawn(crystal.Comp.TeslaSpawnPrototype, Transform(crystal.Owner).Coordinates);
                break;
            default:
                _explosion.TriggerExplosive(crystal.Owner);
                break;
        }
        StationAnnounceIntegrity(crystal, AnnounceIntegrityTypeEnum.Explosion, smState);
    }
}
