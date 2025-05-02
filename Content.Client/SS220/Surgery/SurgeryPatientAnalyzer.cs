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
        var recommendation = new TreatmentRecommendation();

        if (status.OverallDamage > 200)
        {
            // TODO
            recommendation.Problems.Add(Loc.GetString("treatment-recommendation-more-200-damage"));
            //TODO
            recommendation.Operations.Add("shitty-operation-to-prototype");
        }

        if (status.PatientState == Shared.Mobs.MobState.Dead)
        {
            // TODO
            recommendation.Problems.Add(Loc.GetString("treatment-recommendation-mob-state-dead"));
            //TODO
            recommendation.Operations.Add("shitty-operation-to-prototype-2");
        }

        if (status.BodyDecayDegree == 1)
        {
            // TODO
            recommendation.Problems.Add(Loc.GetString("treatment-recommendation-body-near-decay"));
            //TODO
            recommendation.Suggestions.Add("treatment-recommendation-body-near-decay-help");
        }

        if (status.BodyDecayDegree >= 2)
        {
            // TODO
            recommendation.Problems.Add(Loc.GetString("treatment-recommendation-body-decay"));
            //TODO
            recommendation.Suggestions.Add("treatment-recommendation-body-decay-help");
        }

        if (status.BrainRotDegree == 100)
        {
            // TODO
            recommendation.Problems.Add(Loc.GetString("treatment-recommendation-brain-rot"));
            //TODO
            recommendation.Suggestions.Add("treatment-recommendation-brain-rot-help");
        }
        else if (status.BrainRotDegree > 70)
        {
            // TODO
            recommendation.Problems.Add(Loc.GetString("treatment-recommendation-near-brain-rot"));
            //TODO
            recommendation.Suggestions.Add("treatment-recommendation-near-brain-rot-help");
        }

        recommendation.Suggestions.Add("treatment-recommendation-disfunction-healing");

        return recommendation;
    }

    public int GetBrainRotDegree(LimitationReviveComponent comp)
    {
        if (comp.IsDamageTaken)
            return MaxBrainRotPercentage;

        var result = (MaxBrainRotPercentage * (_gameTiming.CurTime - comp.TimeToDamage).Seconds) / comp.DelayBeforeDamage.Seconds;

        return result >= 0 ? result : 0;
    }
}
