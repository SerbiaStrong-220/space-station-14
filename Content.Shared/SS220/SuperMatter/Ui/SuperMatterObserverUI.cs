// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Atmos;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.SuperMatter.Ui;

[Serializable, NetSerializable]
public enum SuperMatterObserverUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class SuperMatterStateUpdate(
                                            int id,
                                            NetEntity? smGridId,
                                            bool isActive,
                                            string name,
                                            float integrity,
                                            float pressure,
                                            float temperature,
                                            (float Value, float Derivative) matter,
                                            (float Value, float Derivative) internalEnergy,
                                            Dictionary<Gas, float> gasRatios, float totalMoles,
                                            (bool Delaminates, TimeSpan ETOfDelamination) delaminate
                                            ) : EntityEventArgs
{
    /// <summary>
    /// Id of SM crystal, uses for handling many SMs. Its server id, for client it false.
    /// </summary>
    public int Id { get; } = id;
    public string Name { get; } = name;
    public NetEntity? SMGridId { get; } = smGridId;
    public bool IsActive { get; } = isActive;
    public float Pressure { get; } = pressure;
    public float Integrity { get; } = integrity;
    public float Temperature { get; } = temperature;
    public (float Value, float Derivative) Matter { get; } = matter;
    public (float Value, float Derivative) InternalEnergy { get; } = internalEnergy;
    public Dictionary<Gas, float> GasRatios { get; } = gasRatios;
    public float TotalMoles { get; } = totalMoles;
    public (bool Delaminates, TimeSpan ETOfDelamination) Delaminate { get; } = delaminate;
}

[Serializable, NetSerializable]
public sealed class SuperMatterStateDeleted(int id) : EntityEventArgs
{
    public int ID = id;
}
