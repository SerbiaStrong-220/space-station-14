using Content.Shared.Store;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Store.Conditions;

public sealed partial class BuyTimeForListingCondition : ListingCondition
{
    [DataField(required: true)]
    public int TimeAmount;

    public override bool Condition(ListingConditionArgs args)
    {
        return IoCManager.Resolve<IGameTiming>().CurTime <= TimeSpan.FromSeconds(TimeAmount); //todo fix
    }
}
