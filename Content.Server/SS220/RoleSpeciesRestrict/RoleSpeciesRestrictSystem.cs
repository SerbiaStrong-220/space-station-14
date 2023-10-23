using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Prototypes;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Server.Preferences.Managers;

namespace Content.Server.SS220.RoleSpeciesRestrict;

public sealed class RoleSpeciesRestrictSystem : EntitySystem
{
    [Dependency] private readonly IServerPreferencesManager _serverPreferences = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public bool IsAllowed(IPlayerSession player, string jobId)
    {
        var job = _prototypes.Index<JobPrototype>(jobId);
        if (job is not null)
        {
            var profile = (_serverPreferences.GetPreferences(player.UserId).SelectedCharacter as HumanoidCharacterProfile)!;
            var species = _prototypes.Index<SpeciesPrototype>(profile.Species);
            if (JobRequirements.TryRequirementsSpeciesMet(job, species, out _, _prototypes))
                return true;
        }
        return false;
    }
}
