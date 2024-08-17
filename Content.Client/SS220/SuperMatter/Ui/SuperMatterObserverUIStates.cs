// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.SS220.SuperMatter.Observer;

namespace Content.Shared.SS220.SuperMatter.Ui;


[Serializable]
public sealed class SuperMatterObserverUpdateState(int id, string name,
                                            float integrity, float pressure,
                                            float temperature,
                                            (float Value, float Derivative) matter,
                                            (float Value, float Derivative) internalEnergy,
                                            (bool Delaminates, TimeSpan ETOfDelamination) delaminate)
                                                     : BoundUserInterfaceState
{
    public int Id { get; } = id;
    public string Name { get; } = name;
    public float Pressure { get; } = pressure;
    public float Integrity { get; } = integrity;
    public float Temperature { get; } = temperature;
    public (float Value, float Derivative) Matter { get; } = matter;
    public (float Value, float Derivative) InternalEnergy { get; } = internalEnergy;
    public (bool Delaminates, TimeSpan ETOfDelamination) Delaminate { get; } = delaminate;
}

[Serializable]
public sealed class SuperMatterObserverInitState(List<Entity<SuperMatterObserverComponent>> observerEntities)
                                                : BoundUserInterfaceState
{
    public List<Entity<SuperMatterObserverComponent>> ObserverEntity { get; } = observerEntities;
}
