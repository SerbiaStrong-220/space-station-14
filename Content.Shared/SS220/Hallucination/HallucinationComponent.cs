// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Hallucination;
// TODO Dictionary -> list
// TODO HallucinationParams -> struct
/// <summary> Never use it yourself! NEVER! Check the server system if you want to work with it. </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class HallucinationComponent : Component
{
    public ProtoId<WeightedRandomEntityPrototype> RandomEntities(int key) => _randomEntities[key];
    public int HallucinationCount => _randomEntities.Count;
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<int, TimeSpan> TotalDurationTimeSpans = new();
    /// <summary> Any operation bool flags goes here </summary>
    public Dictionary<int, bool> EyeProtectionDependent = new();

    [AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<int, (float BetweenHallucinations, float HallucinationMinTime,
                                float HallucinationMaxTime, float TotalDuration)> TimeParams = new();
    [AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    private Dictionary<int, ProtoId<WeightedRandomEntityPrototype>> _randomEntities = new();

    /// <summary> for Key use the id of author/performing entity </summary>
    public HallucinationComponent(int key, ProtoId<WeightedRandomEntityPrototype> randomEntities,
                                    (float BetweenHallucinations, float HallucinationMinTime,
                                    float HallucinationMaxTime, float TotalDuration)? timeParams = null,
                                    bool eyeProtectionDependent = false)
    {
        _randomEntities = new() { { key, randomEntities } };
        TimeParams = new() { { key, timeParams ?? (10f, 2f, 8f, 20f) } };
        EyeProtectionDependent = new() { { key, eyeProtectionDependent } };
    }
    public void AddToRandomEntities(int key, ProtoId<WeightedRandomEntityPrototype> randomEntities,
                                    (float BetweenHallucinations, float HallucinationMinTime,
                                    float HallucinationMaxTime, float TotalDuration)? timeParams = null,
                                    bool eyeProtectionDependent = false)
    {
        _randomEntities.Add(key, randomEntities);
        TimeParams.Add(key, timeParams ?? (10f, 2f, 8f, 20f));
        EyeProtectionDependent.Add(key, eyeProtectionDependent);
    }
    public void RemoveFromRandomEntities(int key)
    {
        _randomEntities.Remove(key);
        TimeParams.Remove(key);
        EyeProtectionDependent.Remove(key);
        TotalDurationTimeSpans.Remove(key);
    }
    public bool TryFindKey(int key)
    {
        return _randomEntities.ContainsKey(key);
    }
}
