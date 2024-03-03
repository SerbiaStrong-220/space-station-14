// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Missable;

[RegisterComponent]
public sealed partial class MissableComponent : Component
{
}

public abstract partial class MissableBaseEvent : HandledEntityEventArgs
{
    public bool ShouldBeMiss = false;
}
public sealed partial class MissableCanMissEvent : MissableBaseEvent { };
public sealed partial class MissableMissChanceBonusEvent : MissableBaseEvent
{
    public List<float> BonusMiss = new();
};