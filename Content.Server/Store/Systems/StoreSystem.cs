using Content.Server.Store.Components; // SS220 Changeling
using Content.Server.StoreDiscount.Systems;
using Content.Server.Traitor.Uplink;
using Content.Shared.Implants.Components;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.UserInterface;
using Robust.Shared.Utility;

namespace Content.Server.Store.Systems;

public sealed partial class StoreSystem : SharedStoreSystem
{
    [Dependency] private readonly StoreDiscountSystem _discount = default!; //SS220-nukeops-discount

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoreComponent, ActivatableUIOpenAttemptEvent>(OnStoreOpenAttempt);
        SubscribeLocalEvent<StoreComponent, BeforeActivatableUIOpenEvent>(BeforeActivatableUiOpen);
        SubscribeLocalEvent<StoreComponent, BoundUserInterfaceMessageAttempt>(OnStoreBoundUiMessageAttempt); // SS220 Changeling

        SubscribeLocalEvent<StoreComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StoreComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StoreComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<RemoteStoreComponent, OpenUplinkImplantEvent>(OnImplantActivate);


        InitializeUi();
        InitializeCommand();
        InitializeRefund();
    }

    private void OnMapInit(EntityUid uid, StoreComponent component, MapInitEvent args)
    {
        RefreshAllListings(component);
        component.StartingMap = Transform(uid).MapUid;
        // TODO UPSTREAM: maybe remove, cause off fixed
        _discount.TryAddDiscounts((uid, component)); //SS220-nukeops-discount

        // Add the bui key if it does not exist already (the check is needed to make sure that we don't overwrite existing InterfaceData).
        if (!UI.HasUi(uid, StoreUiKey.Key))
            UI.SetUi(uid, StoreUiKey.Key, new InterfaceData("StoreBoundUserInterface"));
    }

    private void OnStartup(EntityUid uid, StoreComponent component, ComponentStartup args)
    {
        // for traitors, because the StoreComponent for the PDA can be added at any time.
        if (MetaData(uid).EntityLifeStage == EntityLifeStage.MapInitialized)
        {
            RefreshAllListings(component);
        }

        // SS220 Changeling-begin
        // Dynamically-added intrinsic stores (for example a changeling moving to a new body) do not receive
        // MapInit again, so their BUI must also be prepared here.
        if (!UI.HasUi(uid, StoreUiKey.Key))
            UI.SetUi(uid, StoreUiKey.Key, new InterfaceData("StoreBoundUserInterface"));
        // SS220 Changeling-end

        var ev = new StoreAddedEvent();
        RaiseLocalEvent(uid, ref ev, true);
    }

    private void OnShutdown(EntityUid uid, StoreComponent component, ComponentShutdown args)
    {
        var ev = new StoreRemovedEvent();
        RaiseLocalEvent(uid, ref ev, true);
    }

    // SS220 Changeling-begin
    /// <summary>
    /// Rebinds refund bookkeeping after a store and its bought entities move to a different owner.
    /// </summary>
    public void RetargetBoughtEntities(StoreComponent component, EntityUid newStore)
    {
        foreach (var bought in component.BoughtEntities)
        {
            if (TryComp<StoreRefundComponent>(bought, out var refund))
                refund.StoreEntity = newStore;
        }
    }
    // SS220 Changeling-end

    private void OnStoreOpenAttempt(EntityUid uid, StoreComponent component, ActivatableUIOpenAttemptEvent args)
    {
        if (CanUseOwnerOnlyStore(uid, component, args.User, args.Silent, requireSelfForUnclaimed: false))
            return;

        args.Cancel();
    }

    // SS220 Changeling-begin
    /// <summary>
    /// Raw BUI open messages bypass <see cref="ActivatableUIOpenAttemptEvent"/>. Revalidate owner-only stores at
    /// the actual network input boundary so a nearby client cannot subscribe itself and spend another account.
    /// </summary>
    private void OnStoreBoundUiMessageAttempt(
        Entity<StoreComponent> ent,
        ref BoundUserInterfaceMessageAttempt args)
    {
        if (!args.UiKey.Equals(StoreUiKey.Key) ||
            CanUseOwnerOnlyStore(ent.Owner, ent.Comp, args.Actor, silent: true, requireSelfForUnclaimed: true))
        {
            return;
        }

        args.Cancel();
    }

    private bool CanUseOwnerOnlyStore(
        EntityUid uid,
        StoreComponent component,
        EntityUid user,
        bool silent,
        bool requireSelfForUnclaimed)
    {
        if (!component.OwnerOnly)
            return true;

        if (!Mind.TryGetMind(user, out var mind, out _))
            return false;

        if (component.AccountOwner == null)
        {
            // Intrinsic stores live on their user. Other owner-only stores must first pass their normal
            // activatable-UI ownership flow, which binds AccountOwner before the BUI message is accepted.
            if (requireSelfForUnclaimed && user != uid)
                return false;

            component.AccountOwner = mind;
        }

        if (component.AccountOwner == mind)
            return true;

        if (!silent)
            Popup.PopupEntity(Loc.GetString("store-not-account-owner", ("store", uid)), uid, user);

        return false;
    }
    // SS220 Changeling-end

    private void OnImplantActivate(Entity<RemoteStoreComponent> entity, ref OpenUplinkImplantEvent args)
    {
        if (GetRemoteStore(entity.AsNullable()) is not { } store)
            return;

        ToggleUi(args.Performer, store, store.Comp, entity, entity.Comp);
    }
}
