// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Virology;


[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public sealed partial class VirusData : ReagentData
{
    public List<VirusDescriptor> Viruses = [];

    public static VirusData? From(ReagentId reagent)
    {
        if (reagent.Data is not { } data)
            return null;

        foreach (var entry in data)
        {
            if (entry is VirusData virusData)
                return virusData;
        }

        return null;
    }

    public static IEnumerable<VirusDescriptor> EnumerateViruses(Solution solution)
    {
        foreach (var quantity in solution.Contents)
        {
            if (From(quantity.Reagent) is not { } virusData)
                continue;

            foreach (var virus in virusData.Viruses)
                yield return virus;
        }
    }

    public override ReagentData Clone()
    {
        var viruses = new List<VirusDescriptor>(Viruses.Count);
        foreach (var virus in Viruses)
            viruses.Add(virus.Clone());

        return new VirusData { Viruses = viruses };
    }

    public override bool Equals(ReagentData? other)
    {
        if (other is not VirusData otherData || otherData.Viruses.Count != Viruses.Count)
            return false;

        if (Viruses.Count <= 1)
            return Viruses.Count == 0 || StrainEquals(Viruses[0], otherData.Viruses[0]);

        var unmatched = new List<VirusDescriptor>(otherData.Viruses);
        foreach (var virus in Viruses)
        {
            var matched = false;
            for (var i = 0; i < unmatched.Count; i++)
            {
                if (!StrainEquals(virus, unmatched[i]))
                    continue;

                unmatched.RemoveAt(i);
                matched = true;
                break;
            }

            if (!matched)
                return false;
        }

        return true;
    }

    private static bool StrainEquals(VirusDescriptor a, VirusDescriptor b)
    {
        return a.Source == b.Source && SymptomsEqual(a.Symptoms, b.Symptoms);
    }

    // Same strain compares equal regardless of symptom order, symptoms are unique within a strain.
    private static bool SymptomsEqual(List<VirusSymptomSnapshot> a, List<VirusSymptomSnapshot> b)
    {
        if (a.Count != b.Count)
            return false;

        foreach (var symptom in a)
        {
            var found = false;
            foreach (var other in b)
            {
                if (other.Symptom == symptom.Symptom)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
                return false;
        }

        return true;
    }

    public override int GetHashCode()
    {
        var combined = 0;
        foreach (var virus in Viruses)
        {
            var virusHash = virus.Source?.GetHashCode() ?? 0;
            foreach (var symptom in virus.Symptoms)
                virusHash ^= symptom.Symptom.Id.GetHashCode();

            unchecked
            {
                combined += virusHash;
            }
        }

        return HashCode.Combine(Viruses.Count, combined);
    }
}
