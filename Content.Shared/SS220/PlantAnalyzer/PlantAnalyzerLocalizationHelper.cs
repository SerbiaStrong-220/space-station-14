// Taken from: https://github.com/space-wizards/space-station-14/pull/34600

using Content.Shared.Atmos;
using Content.Shared.Atmos.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Localizations;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.PlantAnalyzer;

public sealed class PlantAnalyzerLocalizationHelper : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public string GasesToLocalizedStrings(List<Gas> gases)
    {
        if (gases.Count == 0)
            return "";

        List<int> gasIds = [];
        foreach (var gas in gases)
            gasIds.Add((int)gas);

        List<string> gasesLoc = [];
        foreach (var gas in _prototypeManager.EnumeratePrototypes<GasPrototype>())
            if (gasIds.Contains(int.Parse(gas.ID)))
                gasesLoc.Add(Loc.GetString(gas.Name));

        return ContentLocalizationManager.FormatList(gasesLoc);
    }

    public string ChemicalsToLocalizedStrings(List<string> ids)
    {
        if (ids.Count == 0)
            return "";

        List<string> locStrings = [];
        foreach (var id in ids)
            locStrings.Add(_prototypeManager.TryIndex<ReagentPrototype>(id, out var prototype) ? prototype.LocalizedName : id);

        return ContentLocalizationManager.FormatList(locStrings);
    }

    public (string Singular, string Plural, string First) ProduceToLocalizedStrings(List<EntProtoId> ids)
    {
        if (ids.Count == 0)
            return ("", "", "");

        List<string> singularStrings = [];
        List<string> pluralStrings = [];
        foreach (var id in ids)
        {
            var singular = _prototypeManager.TryIndex(id, out var prototype) ? prototype.Name : id.Id;
            var plural = Loc.GetString("plant-analyzer-produce-plural", ("thing", singular));

            singularStrings.Add(singular);
            pluralStrings.Add(plural);
        }

        return (
            ContentLocalizationManager.FormatListToOr(singularStrings),
            ContentLocalizationManager.FormatListToOr(pluralStrings),
            singularStrings[0]
        );
    }

    public string MutationsToLocalizedStrings(bool immutable, bool ligneous, bool canScream)
    {
        var mutations = new List<string>();

        if (immutable)
            mutations.Add(Loc.GetString("plant-analyzer-mutation-immutable"));
        if (ligneous)
            mutations.Add(Loc.GetString("plant-analyzer-mutation-ligneous"));
        if (canScream)
            mutations.Add(Loc.GetString("plant-analyzer-mutation-canscream"));

        if (mutations.Count == 0)
            return Loc.GetString("plant-analyzer-mutation-none");

        return ContentLocalizationManager.FormatList(mutations);
    }

    public string HarvestTypeToLocalizedStrings(byte harvestType)
    {
        return harvestType switch
        {
            0 => "norepeat",
            1 => "repeat",
            2 => "selfharvest",
            _ => "other"
        };
    }
}
