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

        return _whitelistSystem.CheckBothPrototype(entityProto, trait.Blacklist, trait.Whitelist);
    }
}

