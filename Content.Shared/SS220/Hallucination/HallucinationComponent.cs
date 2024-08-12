// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Hallucination;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class HallucinationComponent : Component
{
    public ProtoId<WeightedRandomEntityPrototype> RandomEntities(int key) => _randomEntities[key];
    public int HallucinationCount => _randomEntities.Count;
    [DataField, AutoNetworkedField]
    private Dictionary<int, ProtoId<WeightedRandomEntityPrototype>> _randomEntities;
    [DataField, AutoNetworkedField]
    public Dictionary<int, (float BetweenHallucinations, float HallucinationMinTime,
                                float HallucinationMaxTime, float TotalDuration)> TimeParams;
    public Dictionary<int, TimeSpan> NextUpdateTimes;

    /// <summary> for Key use the id of author/performing entity </summary>
    public HallucinationComponent(int key, ProtoId<WeightedRandomEntityPrototype> randomEntities)
    {
        _randomEntities = new() { { key, randomEntities } };
        TimeParams = new() { { key, (10f, 2f, 8f, 20f) } };
        NextUpdateTimes = new() { { key, default! } };
    }
    public void AddToRandomEntities(int key, ProtoId<WeightedRandomEntityPrototype> randomEntities)
    {
        _randomEntities.Add(key, randomEntities);
        TimeParams.Add(key, (10f, 2f, 8f, 20f));
        NextUpdateTimes = new() { { key, default! } };
    }
    public void RemoveFromRandomEntities(int key)
    {
        _randomEntities.Remove(key);
        TimeParams.Remove(key);
        NextUpdateTimes.Remove(key);
    }
}
