using System.Linq;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Store;
using Content.Shared.Store.Components;

namespace Content.Server.SS220.Store.Conditions;

/// <summary>
/// Condition that limits the stock availability of a listing based on the percentage of traitors.
/// </summary>
public sealed partial class ListingStockLimitByTraitorsCondition : ListingCondition
{
    private GameTicker _gameTicker;

    [DataField(required: true)]
    public float ContractorPercentage;

    public override bool Condition(ListingConditionArgs args)
    {
        _gameTicker = args.EntityManager.System<GameTicker>();

        if (!_gameTicker.IsGameRuleAdded<TraitorRuleComponent>())
            return false;

        var totalPurchases = 0;
        var storeCounts = args.EntityManager.EntityQuery<StoreComponent>();

        foreach (var store in storeCounts)
        {
            var listing = store.FullListingsCatalog.FirstOrDefault(x => x.ID == args.Listing.ID);
            if (listing != null)
            {
                totalPurchases += listing.PurchaseAmount;
            }
        }

        var ruleEntities = _gameTicker.GetAddedGameRules();

        foreach (var ruleEnt in ruleEntities)
        {
            if (!args.EntityManager.TryGetComponent<TraitorRuleComponent>(ruleEnt, out var traitorComp))
                continue;

            var maxContractors = Math.Ceiling(traitorComp.TotalTraitors * ContractorPercentage); // round up

            return totalPurchases < maxContractors;
        }

        return false;
    }
}
