using Content.Shared.FixedPoint;

namespace Content.Server.SS220.Contractor;

[RegisterComponent]
public sealed partial class ContractorWarpPointComponent : Component
{
    [DataField]
    public string LocationName;

    [DataField]
    public FixedPoint2 AmountTc;

    [DataField]
    public string Difficulty;
}
