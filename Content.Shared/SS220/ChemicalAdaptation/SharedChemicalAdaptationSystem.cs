// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.ChemicalAdaptation;

/// <summary>
/// This handles limiting the number of defibrillator resurrections
/// </summary>
public abstract class SharedChemicalAdaptationSystem : EntitySystem
{
    public bool TryModifyValue(EntityUid ent, string reagent, ref int value)
    {
        if (!TryComp<ChemicalAdaptationComponent>(ent, out var adaptComp))
            return false;

        if (!adaptComp.ChemicalAdaptations.TryGetValue(reagent, out var adaptationInfo))
            return false;

        value = (int)(value * adaptationInfo.Modifier);

        return true;
    }

    public bool TryModifyValue(EntityUid ent, string reagent, ref float value)
    {
        if (!TryComp<ChemicalAdaptationComponent>(ent, out var adaptComp))
            return false;

        if (!adaptComp.ChemicalAdaptations.TryGetValue(reagent, out var adaptationInfo))
            return false;

        value = value * adaptationInfo.Modifier;

        return true;
    }

    public bool TryModifyValue(EntityUid ent, string reagent, ref TimeSpan value)
    {
        if (!TryComp<ChemicalAdaptationComponent>(ent, out var adaptComp))
            return false;

        if (!adaptComp.ChemicalAdaptations.TryGetValue(reagent, out var adaptationInfo))
            return false;

        value = value * adaptationInfo.Modifier;

        return true;
    }
}
