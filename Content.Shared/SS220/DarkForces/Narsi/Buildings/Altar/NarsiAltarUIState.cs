using Robust.Shared.Serialization;

namespace Content.Shared.SS220.DarkForces.Narsi.Buildings.Altar;

[Serializable, NetSerializable]
public enum NarsiAltarInterfaceKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class NarsiAltarUIState : BoundUserInterfaceState
{
    public int BloodScore;

    public NarsiAltarUIState(int bloodScore)
    {
        BloodScore = bloodScore;
    }
}

[Serializable, NetSerializable]
public sealed class NarsiAltarOpenAbilities : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class NarsiAltarOpenRituals : BoundUserInterfaceMessage
{
}
