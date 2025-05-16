// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;
using Content.Shared.Mobs;

namespace Content.Client.SS220.Surgery;

public struct PatientStatusData
{
    public MobState PatientState = MobState.Invalid;

    public FixedPoint2 OverallDamage = -1;
    public int BrainRotDegree = -1;
    public int BodyDecayDegree = -1;

    public PatientStatusData()
    {
    }
}

public struct TreatmentRecommendation
{
    /// <summary>
    /// Short list of problems, localized.
    /// </summary>
    public List<string> Problems = [];
    /// <summary>
    /// Operations name to help with it.
    /// </summary>
    public List<string> Operations = [];
    /// <summary>
    /// Some hints. Honestly It is skill issue holder.
    /// </summary>
    public List<string> Suggestions = [];

    public TreatmentRecommendation()
    {
    }
}
