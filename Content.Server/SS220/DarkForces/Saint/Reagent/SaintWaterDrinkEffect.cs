using Content.Server.SS220.DarkForces.Saint.Reagent.Events;
using SharedSaintWaterDrinkEvent = Content.Shared.SS220.EntityEffects.Events.OnSaintWaterDrinkEvent;

namespace Content.Server.SS220.DarkForces.Saint.Reagent;

public sealed partial class SaintWaterEffectSystem
{
    // The drink effect itself is shared; retain the port's server-side cleanup callbacks.
    private void InitializeDrink()
    {
        SubscribeLocalEvent<MetaDataComponent, SharedSaintWaterDrinkEvent>(OnDrink);
    }

    private void OnDrink(EntityUid uid, MetaDataComponent component, SharedSaintWaterDrinkEvent args)
    {
        if (!args.Cancelled)
            RaiseLocalEvent(uid, new OnSaintWaterDrinkEvent(uid, args.SaintWaterAmount));
    }
}
