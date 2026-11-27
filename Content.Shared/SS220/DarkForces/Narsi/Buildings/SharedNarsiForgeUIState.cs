using Robust.Shared.Serialization;

namespace Content.Shared.SS220.DarkForces.Narsi.Buildings;

[Serializable, NetSerializable]
public enum SharedNarsiForgeInterfaceKey
{
    Key
}

[Serializable, NetSerializable]
public enum NarsiForgeVisuals : byte
{
    State
}

[Serializable, NetSerializable]
public enum NarsiForgeState : byte
{
    Idle,
    Working,
    Delay
}

[Serializable, NetSerializable]
public sealed class NarsiForgeUIState(
    NarsiForgeState state,
    int runicPlasteelCount,
    int plasteelCount,
    int steelCount
) : BoundUserInterfaceState
{
    public NarsiForgeState State = state;
    public int RunicPlasteelCount = runicPlasteelCount;
    public int PlasteelCount = plasteelCount;
    public int SteelCount = steelCount;
}

[Serializable, NetSerializable]
public sealed class NarsiForgeCreateItemEvent(
    string itemPrototype,
    string requiredMaterial,
    int cost
) : BoundUserInterfaceMessage
{
    public string ItemPrototype = itemPrototype;
    public string RequiredMaterial = requiredMaterial;
    public int Cost = cost;
}
