﻿using Content.Server.SS220.DarkForces.Saint.Reagent.Events;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.DarkForces.Saint.Reagent;

public sealed partial class SaintWaterDrinkEffect : EntityEffect
{
    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is not EntityEffectReagentArgs reagentArgs)
            return;

        var entityManager = args.EntityManager;

        var saintWaterDrinkEvent = new OnSaintWaterDrinkEvent(reagentArgs.TargetEntity, reagentArgs.Quantity);
        entityManager.EventBus.RaiseLocalEvent(reagentArgs.TargetEntity, saintWaterDrinkEvent);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Помогает бороться с нечистью";
    }
}
