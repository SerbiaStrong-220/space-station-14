// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.CultYogg.AnimalCorruption;
using Content.Server.SS220.CultYogg.Cultists;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.SS220.EntityEffects.Effects;

namespace Content.Server.SS220.EntityEffects.Effects;
/// <summary>
/// Used when someone eats MiGoShroom
/// </summary>
public sealed partial class ChemMiGomyceliumEffectSystem : EntityEffectSystem<MetaDataComponent, ChemMiGomyceliumEffect>
{
    [Dependency] private readonly CultYoggSystem _cultYogg = default!;
    [Dependency] private readonly CultYoggAnimalCorruptionSystem _cultYoggAnimalCorruption = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<ChemMiGomyceliumEffect> args)
    {
        if (TryComp<CultYoggComponent>(entity, out var comp))
        {
            RemComp<CultYoggPurifiedComponent>(entity);

            comp.ConsumedAscensionReagent += args.Scale;
            _cultYogg.TryStartAscensionByReagent(entity, comp);
            return;
        }

        //if its an animal -- corrupt it
        if (!HasComp<HumanoidAppearanceComponent>(entity))
        {
            _cultYoggAnimalCorruption.AnimalCorruption(entity);
        }
    }
}
