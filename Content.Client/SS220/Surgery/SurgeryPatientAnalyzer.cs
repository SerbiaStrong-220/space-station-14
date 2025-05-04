// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Atmos.Rotting;
using Content.Client.SS220.LimitationRevive;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Robust.Shared.Timing;

namespace Content.Client.SS220.Surgery;

public sealed class SurgeryPatientAnalyzer : EntitySystem
{
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

        if (TryComp<LimitationReviveComponent>(target, out var limitationReviveComponent))
            patientStatus.BrainRotDegree = GetBrainRotDegree(limitationReviveComponent);

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
        // Good luck =) (c) Karamelnay Yasheritsa
        var recommendation = new TreatmentRecommendation();

        if (status.OverallDamage > 200)
        {
            recommendation.Problems.Add(Loc.GetString("treatment-recommendation-more-200-damage"));
            recommendation.Operations.Add("treatment-recommendation-more-200-damage-help");
        }

        if (status.PatientState == Shared.Mobs.MobState.Dead)
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

        recommendation.Suggestions.Add("treatment-recommendation-disfunction-healing");

        return recommendation;
    }

    public int GetBrainRotDegree(LimitationReviveComponent comp)
    {
        if (comp.IsDamageTaken)
            return MaxBrainRotPercentage;

        var result = (MaxBrainRotPercentage * (_gameTiming.CurTime - comp.TimeToDamage).Seconds) / (int)comp.DelayBeforeDamage.TotalSeconds;

        return result >= 0 ? result : 0;
    }
}
