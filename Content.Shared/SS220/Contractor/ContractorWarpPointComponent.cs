using Content.Shared.FixedPoint;

namespace Content.Shared.SS220.Contractor;

[RegisterComponent]
public sealed partial class ContractorWarpPointComponent : Component
{
    [DataField]
    public string LocationName;

    [DataField]
    public FixedPoint2 AmountTc;

    [DataField]
    public Difficulty Difficulty;
}

[Serializable]
public enum Difficulty
{
    Easy,
    Medium,
    Hard
}
