// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Virology;

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public sealed partial class VirusVaccineData : ReagentData
{
    /// <summary>Strain identities this vaccine immunises against.</summary>
    public List<string> Strains = [];

    public static VirusVaccineData? From(ReagentId reagent)
    {
        if (reagent.Data is not { } data)
            return null;

        foreach (var entry in data)
        {
            if (entry is VirusVaccineData vaccineData)
                return vaccineData;
        }

        return null;
    }

    public override ReagentData Clone() => new VirusVaccineData { Strains = [.. Strains] };

    public override bool Equals(ReagentData? other)
    {
        if (other is not VirusVaccineData otherData || otherData.Strains.Count != Strains.Count)
            return false;

        foreach (var strain in Strains)
        {
            if (!otherData.Strains.Contains(strain))
                return false;
        }

        return true;
    }

    public override int GetHashCode()
    {
        var combined = 0;
        foreach (var strain in Strains)
        {
            unchecked
            {
                combined += strain.GetHashCode();
            }
        }

        return HashCode.Combine(Strains.Count, combined);
    }
}
