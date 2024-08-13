// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.SuperMatter.Ui;

[Serializable, NetSerializable]
public enum SuperMatterObserverUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class SuperMatterObserverUpdateState : BoundUserInterfaceState
{

}


[Serializable, NetSerializable]
public sealed class SuperMatterStateUpdate(
                                            int id,
                                            string name,
                                            float pressure,
                                            float temperature,
                                            (float Value, float Derivative) matter,
                                            (float Value, float Derivative) internalEnergy,
                                            (bool Delaminates, TimeSpan ETOfDelamination) delaminate
                                            ) : EntityEventArgs
{
    // Id of SM crystal, uses for handling many SMs
    public int Id { get; } = id;
    public string Name { get; } = name;
    public float Pressure { get; } = pressure;
    public float Temperature { get; } = temperature;
    public (float Value, float Derivative) Matter { get; } = matter;
    public (float Value, float Derivative) InternalEnergy { get; } = internalEnergy;
    public (bool Delaminates, TimeSpan ETOfDelamination) Delaminate { get; } = delaminate;
}
