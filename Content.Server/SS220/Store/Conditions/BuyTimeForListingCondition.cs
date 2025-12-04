using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Store;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Store.Conditions;

public sealed partial class BuyTimeForListingCondition : ListingCondition
{
    private IGameTiming _gameTiming;
    private MindSystem _mindSystem;
    private SharedRoleSystem _role;

    [DataField(required: true)]
    public TimeSpan TimeAmount;

    public override bool Condition(ListingConditionArgs args)
    {
        _gameTiming = IoCManager.Resolve<IGameTiming>();
        _mindSystem = args.EntityManager.System<MindSystem>();
        _role = args.EntityManager.System<SharedRoleSystem>();

        _mindSystem.TryGetMind(args.Buyer, out var mind, out _);

        if (!_role.MindHasRole<TraitorRoleComponent>(mind, out var traitorRoleComponent))
            return false;

        var currentTime = _gameTiming.CurTime;

        // TODO: SS220
        return true;

    }
}
