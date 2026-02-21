using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Contractor;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class ContractorWarpPointComponent : Component
{
    [DataField]
    public string LocationName;

    [DataField]
    public FixedPoint2 AmountTc;

    [DataField]
    public Difficulty Difficulty;
}

[Serializable, NetSerializable]
public enum Difficulty
{
    Easy,
    Medium,
    Hard,
}
