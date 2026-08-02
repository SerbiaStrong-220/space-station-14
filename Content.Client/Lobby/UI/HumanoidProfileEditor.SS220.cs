// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Traits;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private bool TraitAllowedSpecies(TraitPrototype trait)
    {
        if (Profile == null)
            return true;

        if (trait.Whitelist == null && trait.Blacklist == null)
            return true;

        if (!_prototypeManager.TryIndex(Profile.Species, out var speciesProto))
            return true;

        if (!_prototypeManager.TryIndex<EntityPrototype>(speciesProto.Prototype, out var entityProto))
            return true;

        if (trait.Blacklist?.Components is { } blacklistComponents)
        {
            foreach (var compName in blacklistComponents)
            {
                if (entityProto.Components.ContainsKey(compName))
                    return false;
            }
        }

        if (trait.Whitelist?.Components is { } whitelistComponents)
        {
            var hasAny = false;
            foreach (var compName in whitelistComponents)
            {
                if (entityProto.Components.ContainsKey(compName))
                {
                    hasAny = true;
                    break;
                }
            }

            if (!hasAny)
                return false;
        }

        return true;
    }
}

