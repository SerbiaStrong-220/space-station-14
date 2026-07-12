// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.EntityEffects;
using Content.Shared.SS220.EntityEffects.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.EntityEffects.Effects;

/// <summary>
/// Used to heal hallucinations
/// </summary>
public sealed partial class ChemRemoveHallucinationsEffectSystem : EntityEffectSystem<MetaDataComponent, ChemRemoveHallucinationsEffect>
{
    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<ChemRemoveHallucinationsEffect> args)
    {
        var ev = new OnChemRemoveHallucinationsEvent();
        RaiseLocalEvent(entity, ev);
    }
}

