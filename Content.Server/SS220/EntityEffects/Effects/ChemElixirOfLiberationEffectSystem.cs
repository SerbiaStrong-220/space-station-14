// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.CultYogg.Cultists;
using Content.Shared.EntityEffects;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.SS220.EntityEffects.Effects;

namespace Content.Server.SS220.EntityEffects.Effects;

public sealed partial class ChemElixirOfLiberationEffectSystem : EntityEffectSystem<CultYoggComponent, ChemElixirOfLiberationEffect>
{
    [Dependency] private readonly CultYoggSystem _cultYogg = default!;

    protected override void Effect(Entity<CultYoggComponent> entity, ref EntityEffectEvent<ChemElixirOfLiberationEffect> args)
    {
        _cultYogg.ResetCultist(entity);
    }
}

