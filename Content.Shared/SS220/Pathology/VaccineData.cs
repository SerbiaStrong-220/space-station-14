// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Pathology;

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public sealed partial class VaccineData : ReagentData
{
    public List<string> Strains = new();

    public static VaccineData? From(ReagentId reagent)
    {
        if (reagent.Data is not { } data)
            return null;

        foreach (var entry in data)
        {
            if (entry is VaccineData vaccineData)
                return vaccineData;
        }

        return null;
    }

    public override ReagentData Clone()
    {
        return new VaccineData { Strains = new(Strains) };
    }

    public override bool Equals(ReagentData? other)
    {
        return other is VaccineData vaccine
               && vaccine.Strains.Count == Strains.Count
               && !Strains.Except(vaccine.Strains).Any();
    }

    public override int GetHashCode()
    {
        var hash = 0;
        foreach (var strain in Strains)
            hash ^= strain.GetHashCode();

        return hash;
    }
}
