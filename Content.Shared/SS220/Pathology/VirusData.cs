// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Pathology;

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public sealed partial class VirusData : ReagentData
{
    public List<VirusInstance> Viruses = new();

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

    /// <summary>Yields every virus carried by any reagent in a solution.</summary>
    public static IEnumerable<VirusInstance> EnumerateViruses(Solution solution)
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
        var viruses = new List<VirusInstance>(Viruses.Count);
        foreach (var virus in Viruses)
            viruses.Add(virus.Clone());

        return new VirusData { Viruses = viruses };
    }

    public override bool Equals(ReagentData? other)
    {
        if (other is not VirusData otherData || otherData.Viruses.Count != Viruses.Count)
            return false;

        // common case: 0 or 1 virus — order is moot, compare directly without allocating
        if (Viruses.Count <= 1)
            return Viruses.Count == 0 || VirusEquals(Viruses[0], otherData.Viruses[0]);

        // multiple viruses - list order isn't canonical, so match set-wise, consuming each match so
        // duplicate strains are still counted. Kept consistent with the order-independent GetHashCode.
        var unmatched = new List<VirusInstance>(otherData.Viruses);
        foreach (var virus in Viruses)
        {
            var matched = false;
            for (var i = 0; i < unmatched.Count; i++)
            {
                if (!VirusEquals(virus, unmatched[i]))
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

    private static bool VirusEquals(VirusInstance a, VirusInstance b)
    {
        return a.Source == b.Source && SymptomsEqual(a.Symptoms, b.Symptoms);
    }

    // samples of the same strain compare equal regardless of symptom order. Symptoms are unique within
    // a virus, so equal counts + every symptom of a present in b means the sets match — no LINQ/alloc.
    private static bool SymptomsEqual(List<ProtoId<PathologyPrototype>> a, List<ProtoId<PathologyPrototype>> b)
    {
        if (a.Count != b.Count)
            return false;

        foreach (var symptom in a)
        {
            if (!b.Contains(symptom))
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
                virusHash ^= symptom.Id.GetHashCode();

            unchecked
            {
                combined += virusHash;
            }
        }

        return HashCode.Combine(Viruses.Count, combined);
    }
}
