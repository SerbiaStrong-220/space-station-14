// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Content.Shared.Inventory;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Hallucination;
/// <summary>
/// lmaoooooo
/// </summary>
[NetworkedComponent]
public abstract partial class SharedHallucinationComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<HallucinationSetting> Hallucinations = [];
}

[Serializable, NetSerializable]
public sealed class HallucinationComponentState : ComponentState
{
    public List<HallucinationSetting> Hallucinations { get; init; } = [];
}
[DataDefinition, Serializable, NetSerializable]
public partial struct HallucinationSetting()
{
    [DataField]
    public ProtoId<WeightedRandomEntityPrototype> RandomEntities;
    /// <summary>
    /// if null nothing can defend from hallucination
    /// </summary>
    [DataField]
    public string? ComponentName;
    /// <summary>
    /// If ItemSlot null we check all slots and even pockets if CheckPockets is true
    /// </summary>
    [DataField]
    public SlotFlags? ItemSlot;
    [DataField]
    public bool CheckPockets;
    [DataField]
    public float BetweenHallucinations;
    [DataField]
    public float HallucinationMinTime;
    [DataField]
    public float HallucinationMaxTime;
    [DataField]
    public float TotalDuration;

    public readonly (string? ComponentName, SlotFlags? ItemSlot, bool CheckPockets) Protection =>
                                                                        (ComponentName, ItemSlot, CheckPockets);
    public readonly (float BetweenHallucinations, float HallucinationMinTime,
            float HallucinationMaxTime, float TotalDuration) TimeParams =>
                                                                        (BetweenHallucinations, HallucinationMinTime,
                                                                        HallucinationMaxTime, TotalDuration);

    public HallucinationSetting(float betweenHallucinations, float hallucinationMinTime,
                        float hallucinationMaxTime, float totalDuration,
                        ProtoId<WeightedRandomEntityPrototype> randomEntities,
                        string? protectionComponent, SlotFlags? itemSlot, bool checkPockets)
                        : this()
    {
        RandomEntities = randomEntities;
        ComponentName = protectionComponent;
        ItemSlot = itemSlot;
        CheckPockets = checkPockets;
        BetweenHallucinations = betweenHallucinations;
        HallucinationMinTime = hallucinationMinTime;
        HallucinationMaxTime = hallucinationMaxTime;
        TotalDuration = totalDuration;
    }

    public bool Equals(HallucinationSetting other)
    {
        return TimeParams.Equals(other.TimeParams)
                && RandomEntities.Equals(other.RandomEntities)
                && Protection.Equals(other.Protection);
    }
}
