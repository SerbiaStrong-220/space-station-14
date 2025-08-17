// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Store.Systems;
using Content.Server.StoreDiscount.Systems;
using Content.Shared.Mind;
using Content.Shared.SS220.NukeOpsDiscount.Components;
using Content.Shared.Store.Components;
using Content.Shared.UserInterface;
using Robust.Server.Containers;
using System.Linq;

namespace Content.Shared.SS220.NukeOpsDiscount.Systems;

public sealed partial class NukeOpsDiscountSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NukeOpsDiscountComponent, BeforeActivatableUIOpenEvent>(OnStoreTrying);
    }
    private void OnStoreTrying(EntityUid uid, NukeOpsDiscountComponent comp, BeforeActivatableUIOpenEvent ev)
    {
        if (comp.IsUsed) return;
        if (!_container.TryGetContainingContainer(uid, out var container)) return;

        if (!_mind.TryGetMind(container.Owner, out var mind, out _))
            return;

        var store = EnsureComp<StoreComponent>(uid);

        store.AccountOwner = mind;

        var uplinkInitializedEvent = new StoreInitializedEvent(
            TargetUser: mind,
            Store: uid,
            UseDiscounts: true,
            Listings: _store.GetAvailableListings(mind, uid, store)
                .ToArray());

        comp.IsUsed = true;

        RaiseLocalEvent(ref uplinkInitializedEvent);
    }
}
