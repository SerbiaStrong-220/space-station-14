// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Atmos.Rotting;
using Content.Client.SS220.LimitationRevive;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.SS220.Pathology;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.SS220.Surgery;

public sealed class SurgeryPatientAnalyzer : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly RottingSystem _rotting = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private const int MaxBrainRotPercentage = 100;

    public PatientStatusData GetStatus(EntityUid target)
    {
        var patientStatus = new PatientStatusData();

        if (TryComp<MobStateComponent>(target, out var mobStateComponent))
            patientStatus.PatientState = mobStateComponent.CurrentState;

        if (TryComp<DamageableComponent>(target, out var damageableComponent))
            patientStatus.OverallDamage = damageableComponent.Damage.GetTotal();

        if (TryComp<RottingComponent>(target, out var rottingComponent))
            patientStatus.BodyDecayDegree = _rotting.RotStage(target, rottingComponent);
        else
            patientStatus.BodyDecayDegree = 0;

        if (TryComp<LimitationReviveComponent>(target, out var limitationReviveComponent)
                && mobStateComponent?.CurrentState == MobState.Dead) // kinda bad fix
            patientStatus.BrainRotDegree = GetBrainRotDegree(limitationReviveComponent, mobStateComponent);

        CollectPathologyDescriptions(target, ref patientStatus);

        return patientStatus;
    }

    public TreatmentRecommendation GetTreatmentRecommendation(EntityUid target)
    {
        return GetTreatmentRecommendation(GetStatus(target));
    }

    public TreatmentRecommendation GetTreatmentRecommendation(PatientStatusData status)
    {
        // Okay lets make it quick
        // This is kinda experimental thing
        // If it become useful you need to do
        // 1. Move to prototypes it.
        // 2. Make ConditionInterface for prototype.
        // Good luck, hf =)
        var recommendation = new TreatmentRecommendation();

        if (status.OverallDamage > 200)
        {
            recommendation.Problems.Add(Loc.GetString("treatment-recommendation-more-200-damage"));
            recommendation.Operations.Add("treatment-recommendation-more-200-damage-help");
        }

        if (status.PatientState == MobState.Dead)
        {
            recommendation.Problems.Add(Loc.GetString("treatment-recommendation-mob-state-dead"));
            recommendation.Operations.Add("treatment-recommendation-mob-state-dead-help");
        }

        if (status.BodyDecayDegree == 1)
        {
            recommendation.Problems.Add(Loc.GetString("treatment-recommendation-body-near-decay"));
            recommendation.Suggestions.Add("treatment-recommendation-body-near-decay-help");
        }

        if (status.BodyDecayDegree >= 2)
        {
            recommendation.Problems.Add(Loc.GetString("treatment-recommendation-body-decay"));
            recommendation.Suggestions.Add("treatment-recommendation-body-decay-help");
        }

        if (status.BrainRotDegree == 100)
        {
            recommendation.Problems.Add(Loc.GetString("treatment-recommendation-brain-rot"));
            recommendation.Suggestions.Add("treatment-recommendation-brain-rot-help");
        }
        else if (status.BrainRotDegree > 70)
        {
            recommendation.Problems.Add(Loc.GetString("treatment-recommendation-near-brain-rot"));
            recommendation.Suggestions.Add("treatment-recommendation-near-brain-rot-help");
        }

        foreach (var descLocId in status.PathologiesDescription)
        {
            recommendation.Problems.Add(Loc.GetString(descLocId));
        }

        return recommendation;
    }

    // TODO: after redoing LimRev shared comp come here
    public int GetBrainRotDegree(LimitationReviveComponent limitationRevive, MobStateComponent mobState)
    {
        if (limitationRevive.DamageCountingTime is not null)
        {
            var result = (MaxBrainRotPercentage * (int)(_gameTiming.CurTime - limitationRevive.DamageCountingTime.Value).TotalSeconds) / (int)limitationRevive.BeforeDamageDelay.TotalSeconds;
            return result >= 0 ? result : 0;
        }

        if (mobState.CurrentState == MobState.Dead)
            return MaxBrainRotPercentage;


        return 0;
    }

    private void CollectPathologyDescriptions(EntityUid target, ref PatientStatusData statusData)
    {
        if (!TryComp<PathologyHolderComponent>(target, out var pathologyHolder))
            return;

        foreach (var (pathologyId, instanceData) in pathologyHolder.ActivePathologies)
        {
            if (!_prototype.Resolve(pathologyId, out var pathologyPrototype))
                continue;

            statusData.PathologiesDescription.Add(pathologyPrototype.Definition[instanceData.Level].Description);
        }
    }
}
