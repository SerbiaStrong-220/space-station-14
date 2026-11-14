using Robust.Shared.Serialization;

namespace Content.Shared.SS220.DarkForces.Narsi.Buildings.Altar.Rituals;

[Serializable, NetSerializable]
public enum NarsiRitualsInterfaceKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class NarsiRitualsState : BoundUserInterfaceState
{
    public NarsiRitualsProgressState RitualsProgressState;
    public List<NarsiRitualCategoryUIModel> RitualsCategories;

    public NarsiRitualsState(List<NarsiRitualCategoryUIModel> ritualsCategories, NarsiRitualsProgressState ritualsProgressState)
    {
        RitualsProgressState = ritualsProgressState;
        RitualsCategories = ritualsCategories;
    }
}

[Serializable, NetSerializable]
public sealed class NarsiAltarStartRitualEvent : BoundUserInterfaceMessage
{
    public string PrototypeId;

    public NarsiAltarStartRitualEvent(string prototypeId)
    {
        PrototypeId = prototypeId;
    }
}
