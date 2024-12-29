// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.SS220.SuperMatterCrystal.Components;
using Content.Server.Tesla.Components;
using Content.Shared.Radiation.Components;
using Content.Shared.Atmos;
using Content.Shared.SS220.SuperMatter.Functions;
using Robust.Shared.Timing;
using Content.Shared.Administration;

namespace Content.Server.SS220.SuperMatterCrystal;
public sealed partial class SuperMatterSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private const float ZapPerEnergy = 55f;
    private const float ZapThreshold = 70f;
    private const float MaxTimeBetweenArcs = 8f;
    private const float MaxTimeDecreaseBetweenArcs = 4f;
    private const int MaxAmountOfArcs = 7;
    private const float ArcsToTimeDecreaseEfficiency = 0.3f;

    private const float RadiationPerEnergy = 70f;

    private const float IntegrityDamageICAnnounceDelay = 12f;
    private const float IntegrityDamageStationAnnouncementDelay = 6f;

    private const float ReleasedEnergyToGasHeat = 60f;

    public override void Initialize()
    {
        base.Initialize();

        InitializeAnnouncement();
        InitializeEventHandler();
        InitializeDatabase();
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        // 0.033f corresponds to 30 ticks per second. Just in case if server got very laggy
        var flooredFrameTime = MathF.Min(frameTime, 0.09f);
        var query = EntityQueryEnumerator<SuperMatterComponent>();
        while (query.MoveNext(out var uid, out var smComp))
        {
            if (!HasComp<MetaDataComponent>(uid)
                || MetaData(uid).Initialized == false)
                continue;

            // add here to give admins a way to freeze all logic
            if (HasComp<AdminFrozenComponent>(uid))
                continue;

            var crystal = new Entity<SuperMatterComponent>(uid, smComp);
            UpdateDelayed(crystal, flooredFrameTime);
            SuperMatterUpdate(crystal, flooredFrameTime);
        }
    }

    private void SuperMatterUpdate(Entity<SuperMatterComponent> crystal, float frameTime)
    {
        crystal.Comp.UpdatesBetweenBroadcast++;
        if (!TryGetCrystalGasMixture(crystal.Owner, out var gasMixture))
        {
            Log.Error($"Got null GasMixture in {crystal}");
            return;
        }
        AddGasesToAccumulator(crystal.Comp, gasMixture);
        crystal.Comp.PressureAccumulator += gasMixture.Pressure;
        if (!crystal.Comp.Activated)
            return;
        // here we ask for values before update SM parameters
        // f.e. we save prev value for broadcast's accumulators
        var prevInternalEnergy = crystal.Comp.InternalEnergy;
        var prevMatter = crystal.Comp.Matter;
        var decayedMatter = CalculateDecayedMatter(crystal, gasMixture) * frameTime;
        // this method make changes in SM parameters!
        EvaluateDeltaInternalEnergy(crystal, gasMixture, frameTime);
        var smState = SuperMatterFunctions.GetSuperMatterPhase(crystal.Comp.Temperature, gasMixture.Pressure);
        var crystalTemperature = crystal.Comp.Temperature;
        var pressure = gasMixture.Pressure;

        var releasedEnergyPerFrame = crystal.Comp.InternalEnergy * GetReleaseEnergyConversionEfficiency(crystalTemperature, pressure)
                        * (SuperMatterGasResponse.GetGasInfluenceReleaseEnergyEfficiency(gasMixture) + 1);
        crystal.Comp.AccumulatedRadiationEnergy += releasedEnergyPerFrame * GetZapToRadiationRatio(crystalTemperature, pressure, smState);
        crystal.Comp.AccumulatedZapEnergy += releasedEnergyPerFrame * (1 - GetZapToRadiationRatio(crystalTemperature, pressure, smState));

        crystal.Comp.InternalEnergy -= releasedEnergyPerFrame;

        EjectGases(decayedMatter, crystalTemperature, smState, gasMixture);
        crystal.Comp.Matter -= decayedMatter;

        _atmosphere.AddHeat(gasMixture, ReleasedEnergyToGasHeat * releasedEnergyPerFrame);
        AddIntegrityDamage(crystal.Comp, GetIntegrityDamage(crystal.Comp) * frameTime);

        // Update Accumulators for Broadcasting to Clients
        crystal.Comp.MatterDervAccumulator = (crystal.Comp.Matter - prevMatter) / frameTime;
        crystal.Comp.InternalEnergyDervAccumulator = (crystal.Comp.InternalEnergy - prevInternalEnergy) / frameTime;
    }
    private void UpdateDelayed(Entity<SuperMatterComponent> crystal, float frameTime)
    {
        if (_gameTiming.CurTime > crystal.Comp.NextOutputEnergySourceUpdate
            && crystal.Comp.Activated)
        {
            ReleaseEnergy(crystal);
            crystal.Comp.AccumulatedRadiationEnergy = 0;
            crystal.Comp.AccumulatedZapEnergy = 0;
            crystal.Comp.NextOutputEnergySourceUpdate = _gameTiming.CurTime + crystal.Comp.OutputEnergySourceUpdateDelay;
        }
        if (_gameTiming.CurTime > crystal.Comp.NextDamageImplementTime)
        {
            if (crystal.Comp.IsDelaminate)
            {
                UpdateDelamination(crystal);
                return;
            }
            if (!TryImplementIntegrityDamage(crystal))
            {
                crystal.Comp.Integrity = 0f;
                MarkAsLaminated(crystal);
            }
            crystal.Comp.IntegrityDamageAccumulator = 0f;

            crystal.Comp.NextDamageImplementTime = _gameTiming.CurTime + TimeSpan.FromSeconds(_broadcastDelay);
            BroadcastData(crystal);
        }
        if (_gameTiming.CurTime > crystal.Comp.NextDamageStationAnnouncement)
        {
            var announceType = GetAnnounceIntegrityType(crystal.Comp);
            RadioAnnounceIntegrity(crystal, announceType);
            crystal.Comp.NextDamageStationAnnouncement = _gameTiming.CurTime + TimeSpan.FromSeconds(IntegrityDamageICAnnounceDelay);
        }
    }
    private void ReleaseEnergy(Entity<SuperMatterComponent> crystal)
    {
        var (crystalUid, smComp) = crystal;

        if (!TryComp<RadiationSourceComponent>(crystalUid, out var radiationSource))
        {
            Log.Error($"SM doesnt has a RadiationSourceComponent, error while updating {crystal}");
            return;
        }
        if (!TryComp<LightningArcShooterComponent>(crystalUid, out var arcShooterComponent))
        {
            Log.Error($"SM doesnt has a LightningArcShooterComponent, error while updating {crystal}");
            return;
        }
        var accumulatedZapEnergyTrashed = smComp.AccumulatedZapEnergy - ZapThreshold;
        var maxAmountOfArcs = Math.Clamp((int)MathF.Round(accumulatedZapEnergyTrashed / ZapPerEnergy), 0, MaxAmountOfArcs);
        var timeDecreaseBetweenArcs = Math.Clamp((accumulatedZapEnergyTrashed / ZapPerEnergy - MaxAmountOfArcs)
                                                    * ArcsToTimeDecreaseEfficiency, 0f, MaxTimeDecreaseBetweenArcs);
        if (maxAmountOfArcs == 0)
            arcShooterComponent.Enabled = false;
        else
        {
            arcShooterComponent.Enabled = true;
            arcShooterComponent.MaxLightningArc = maxAmountOfArcs;
            arcShooterComponent.ShootMaxInterval = MaxTimeBetweenArcs - timeDecreaseBetweenArcs;
        }

        var radiationIntensity = smComp.AccumulatedRadiationEnergy / RadiationPerEnergy;
        radiationSource.Intensity = radiationIntensity;
    }
    private void EjectGases(float decayedMatter, float crystalTemperature, SuperMatterPhaseState smState, GasMixture gasMixture)
    {
        var pressure = gasMixture.Pressure;
        var matter = MathF.Max(decayedMatter, 0f);
        var oxygenMoles = matter * GetOxygenToPlasmaRatio(crystalTemperature, pressure, smState);
        var plasmaMoles = matter * (1 - GetOxygenToPlasmaRatio(crystalTemperature, pressure, smState));
        var heatEnergy = GetChemistryPotential(crystalTemperature, gasMixture.Pressure) * matter / MatterNondimensionalization;
        var releasedGasMixture = new GasMixture(gasMixture.Volume) { Temperature = crystalTemperature };
        releasedGasMixture.SetMoles(Gas.Oxygen, oxygenMoles);
        releasedGasMixture.SetMoles(Gas.Plasma, plasmaMoles);
        _atmosphere.Merge(gasMixture, releasedGasMixture);
        _atmosphere.AddHeat(gasMixture, heatEnergy);
    }
}
