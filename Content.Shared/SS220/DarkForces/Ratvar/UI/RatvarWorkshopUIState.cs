using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.DarkForces.Ratvar.UI;

[Serializable, NetSerializable]
public enum RatvarWorkshopKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class RatvarWorkshopUIState : BoundUserInterfaceState
{
    public int Brass;
    public int Power;
    public bool InProgress;

    public RatvarWorkshopUIState(int brass, int power, bool inProgress)
    {
        Brass = brass;
        Power = power;
        InProgress = inProgress;
    }
}

[Serializable, NetSerializable]
public sealed class RatvarWorkshopCraftSelected : BoundUserInterfaceMessage
{
    public int Brass;
    public int Power;
    public int CraftTime;
    public EntProtoId EntityProduce;

    public RatvarWorkshopCraftSelected(EntProtoId entityProduce, int brass, int power, int craftTime)
    {
        EntityProduce = entityProduce;
        Brass = brass;
        Power = power;
        CraftTime = craftTime;
    }
}
