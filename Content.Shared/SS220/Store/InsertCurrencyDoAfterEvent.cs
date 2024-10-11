// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Content.Shared.Store.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Store;

[Serializable, NetSerializable]
public sealed partial class InsertCurrencyDoAfterEvent : DoAfterEvent
{
    // SERIALIZATION FUCKERY DOWN THERE
    /// <summary>
    /// Used to store EntityUid of currncy to avoid using Entity<CurrencyCompoenent?>, cuz it's server-side only.
    /// </summary>
    [NonSerialized]
    public EntityUid Currency;

    [NonSerialized]
    public Entity<StoreComponent?> Store;

    public InsertCurrencyDoAfterEvent(EntityUid currency, Entity<StoreComponent?> store)
    {
        Currency = currency;
        Store = store;
    }

    public override DoAfterEvent Clone() => this;
}
