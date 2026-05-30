using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Server.SS220.Discord;

namespace Content.Server.SS220.Sponsors;

public sealed class SponsorLoadoutSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly DiscordPlayerManager _discordManager = default!;

    private static readonly Dictionary<string, string[]> SurvivalReplacements = new()
    {
        { "Survival",          ["EmergencyOxygenSponsor", "EmergencyNitrogenSponsor", "LoadoutSpeciesVoxNitrogen"]                   },
        { "SurvivalClown",     ["EmergencyOxygenClownSponsor", "EmergencyNitrogenClownSponsor", "LoadoutSpeciesVoxNitrogen"]         },
        { "SurvivalMime",      ["EmergencyOxygenMimeSponsor", "EmergencyNitrogenMimeSponsor", "LoadoutSpeciesVoxNitrogen"]           },
        { "SurvivalSecurity",  ["EmergencyOxygenSecuritySponsor", "EmergencyNitrogenSecuritySponsor", "LoadoutSpeciesVoxNitrogen"]   },
        { "SurvivalExtended",  ["EmergencyOxygenExtendedSponsor", "EmergencyNitrogenExtendedSponsor", "LoadoutSpeciesVoxNitrogen"]   },
        { "SurvivalMedical",   ["EmergencyOxygenMedicalSponsor", "EmergencyNitrogenMedicalSponsor", "LoadoutSpeciesVoxNitrogen"]     },
        { "SurvivalBrigMedic", ["EmergencyOxygenBrigmedicSponsor", "EmergencyNitrogenBrigmedicSponsor", "LoadoutSpeciesVoxNitrogen"] },
    };

    public void ApplySponsorSurvival(RoleLoadout loadout, HumanoidCharacterProfile profile, ICommonSession? session)
    {
        var collection = IoCManager.Instance!;

        // Проверка Спонсорства
        if (session == null || !_discordManager.TryGetSponsorTierFromCache(session.UserId, out _))
            return;

        foreach (var (standardGroup, sponsorLoadouts) in SurvivalReplacements)
        {
            ProtoId<LoadoutGroupPrototype> groupKey = standardGroup;

            if (!loadout.SelectedLoadouts.ContainsKey(groupKey))
                continue;

            var newSelections = new List<Loadout>();

            foreach (var loadoutId in sponsorLoadouts)
            {
                ProtoId<LoadoutPrototype> protoId = loadoutId;

                if (!_protoMan.HasIndex<LoadoutPrototype>(protoId))
                    continue;

                if (!loadout.IsValid(profile, session, protoId, collection, out _))
                    continue;

                newSelections.Add(new Loadout { Prototype = protoId });
            }

            loadout.SelectedLoadouts[groupKey] = newSelections;
        }
    }
}