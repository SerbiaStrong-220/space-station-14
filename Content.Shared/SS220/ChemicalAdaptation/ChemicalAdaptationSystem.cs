// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.StatusEffect;

namespace Content.Shared.SS220.ChemicalAdaptation;

public abstract class SharedChemicalAdaptationSystem : EntitySystem
{
    [ValidatePrototypeId<StatusEffectPrototype>]
    public const string ChemicalAdaptationKey = "ChemicalAdaptation";
}
