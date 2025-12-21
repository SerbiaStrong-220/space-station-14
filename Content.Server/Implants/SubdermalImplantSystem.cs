using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Implants;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.SS220.Store;
using Content.Shared.Store.Components;

namespace Content.Server.Implants;

public sealed class SubdermalImplantSystem : SharedSubdermalImplantSystem
{
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoreComponent, ImplantRelayEvent<AfterInteractUsingEvent>>(OnStoreRelay);
    }

    // TODO: This shouldn't be in the SubdermalImplantSystem
    private void OnStoreRelay(EntityUid uid, StoreComponent store, ImplantRelayEvent<AfterInteractUsingEvent> implantRelay)
    {
        var args = implantRelay.Event;

        if (args.Handled)
            return;

        // can only insert into yourself to prevent uplink checking with renault
        if (args.Target != args.User)
            return;

        if (!TryComp<CurrencyComponent>(args.Used, out var currency))
            return;

        //SS220-insert-currency-doafter begin
        if (store.CurrencyInsertTime != null)
        {
            var doAfter = new DoAfterArgs(EntityManager, args.User, store.CurrencyInsertTime.Value,
                new InsertCurrencyDoAfterEvent(args.Used, (uid, store)),
                uid)
            {
                NeedHand = true,
                BreakOnDamage = true
            };

            _doAfter.TryStartDoAfter(doAfter);
            args.Handled = true;
            return;
        }
        //SS220-insert-currency-doafter end

        // same as store code, but message is only shown to yourself
        if (!_store.TryAddCurrency((args.Used, currency), (uid, store)))
            return;

        args.Handled = true;
        var msg = Loc.GetString("store-currency-inserted-implant", ("used", args.Used));
        _popup.PopupEntity(msg, args.User, args.User);
    }
}
