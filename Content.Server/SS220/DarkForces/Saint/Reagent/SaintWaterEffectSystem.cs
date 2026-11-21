using Content.Server.SS220.DarkForces.Saint.Reagent.Events;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;

namespace Content.Server.SS220.DarkForces.Saint.Reagent;

public sealed class SaintWaterEffectSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MetaDataComponent, EntityEffectEvent<SaintWaterDrinkEffect>>(OnDrink);
        SubscribeLocalEvent<MetaDataComponent, EntityEffectEvent<SaintWaterFlammableEffect>>(OnFlammable);
    }

    private void OnDrink(Entity<MetaDataComponent> ent, ref EntityEffectEvent<SaintWaterDrinkEffect> args)
    {
        RaiseLocalEvent(ent.Owner, new OnSaintWaterDrinkEvent(ent.Owner, FixedPoint2.New(args.Scale)));
    }

    private void OnFlammable(Entity<MetaDataComponent> ent, ref EntityEffectEvent<SaintWaterFlammableEffect> args)
    {
        RaiseLocalEvent(ent.Owner, new OnSaintWaterFlammableEvent(ent.Owner, FixedPoint2.New(args.Scale), null));
    }
}
