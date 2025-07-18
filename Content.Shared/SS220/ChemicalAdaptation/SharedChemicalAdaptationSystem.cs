// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.ChemicalAdaptation;

public abstract class SharedChemicalAdaptationSystem : EntitySystem
{
    public virtual bool TryModifyValue(EntityUid ent, string reagent, ref int value)
    {
        return false;
    }

    public virtual bool TryModifyValue(EntityUid ent, string reagent, ref float value)
    {
        return false;
    }

    public virtual bool TryModifyValue(EntityUid ent, string reagent, ref TimeSpan value)
    {
        return false;
    }
}
