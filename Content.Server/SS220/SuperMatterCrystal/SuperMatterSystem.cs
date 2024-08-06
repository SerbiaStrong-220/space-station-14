// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Chat.Managers;
using Content.Server.SS220.SuperMatterCrystal.Components;
using Robust.Shared.Timing;

namespace Content.Server.SS220.SuperMatterCrystal;

public sealed partial class SuperMatterSystem : EntitySystem
{
    [Dependency] IChatManager _chatManager = default!;
    [Dependency] IGameTiming _gameTiming = default!;
    public override void Initialize()
    {
        base.Initialize();

        // subscribe FCK EVENTS
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<SuperMatterComponent>();
        while (query.MoveNext(out var uid, out var smComp))
        {
            var crystal = new Entity<SuperMatterComponent>(uid, smComp);
            UpdateSuperMatter(crystal, frameTime);
        }
    }

    private void UpdateSuperMatter(Entity<SuperMatterComponent> crystal, float frameTime)
    {
        if (!crystal.Comp.Activated)
            return;

        if (!TryGetCrystalGasMixture(crystal.Owner, out var gasMixture))
        {
            Log.Error($"Got null GasMixture in {crystal}, changed SM state to ErrorState");
            return;
        }

        var decayedMatter = CalculateDecayedMatter(crystal, gasMixture);
        EvaluateDeltaInternalEnergy(crystal, gasMixture, frameTime);

        var smState = GetSuperMatterPhase(crystal, gasMixture);
        var temperature = crystal.Comp.Temperature;
        var pressure = gasMixture.Pressure;

        var releasedEnergy = crystal.Comp.InternalEnergy * GetReleaseEnergyConversionEfficiency(temperature, pressure);
        var ZapEnergy = releasedEnergy * GetZapToRadiationRatio(temperature, pressure, smState);
        var RadiationEnergy = releasedEnergy * (1 - GetZapToRadiationRatio(temperature, pressure, smState));

        var O2Moles = decayedMatter * GetO2ToPlasmaRatio(temperature, pressure, smState);
        var PlasmaMoles = decayedMatter * (1 - GetO2ToPlasmaRatio(temperature, pressure, smState));
    }
}
