using Content.Shared.Store;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pirates;

[RegisterComponent]
public sealed partial class PirateGameRuleComponent : Component
{
    [DataField]
    public int MaximumRecruits = 3;

    [ViewVariables]
    public int SuccessfulRecruits;

    [ViewVariables]
    public int TotalItemsSold;

    [ViewVariables]
    public long TotalLootValue;

    [ViewVariables]
    public List<EntityUid> CaptureTargets = new();

    [ViewVariables]
    public Dictionary<ProtoId<ListingPrototype>, int> MarketPurchases = new();
}
