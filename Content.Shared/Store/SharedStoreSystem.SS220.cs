using Content.Shared.SS220.Store;
using Content.Shared.Store.Components;

namespace Content.Shared.Store;

/// <summary>
/// Manages general interactions with a store and different entities,
/// getting listings for stores, and interfacing with the store UI.
/// </summary>
public abstract partial class SharedStoreSystem : EntitySystem
{
    private void OnInsertCurrencyDoAfter(Entity<StoreComponent> store, ref InsertCurrencyDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used is not { } used)
            return;

        TryGetEntity(args.TargetOverride, out var targetOverride);
        if (args.TargetOverride is not null && targetOverride is null)
            return;

        if (!TryComp<CurrencyComponent>(used, out var currencyComp))
            return;

        if (!TryAddCurrency((used, currencyComp), store!))
            return;

        args.Handled = true;
        var msg = Loc.GetString("store-currency-inserted", ("used", used), ("target", targetOverride ?? store));
        Popup.PopupEntity(msg, store, args.User);
    }
}

