﻿// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.EntityEffects;
using Content.Shared.SS220.EntityEffects.Events;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.DarkForces.Saint.Reagent;

public sealed partial class SaintWaterDrinkEffectSystem : EntityEffectSystem<MetaDataComponent, SaintWaterDrinkEffect>
{
    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<SaintWaterDrinkEffect> args)
    {
        var saintWaterDrinkEvent = new OnSaintWaterDrinkEvent(entity, args.Scale);
        RaiseLocalEvent(entity, saintWaterDrinkEvent);
    }
}
