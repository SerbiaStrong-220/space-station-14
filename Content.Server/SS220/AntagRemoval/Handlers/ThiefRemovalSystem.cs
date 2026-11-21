// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Linq;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Strip.Components;

namespace Content.Server.SS220.AntagRemoval.Handlers;

public sealed class ThiefRemovalSystem : EntitySystem, IAntagRemovalHandler
{
    public bool RemovesMindRole => false;

    public bool CanRemove(EntityUid body, EntityUid mindId, out string reason)
    {
        reason = string.Empty;
        return true;
    }

    public bool TryRemove(EntityUid body, EntityUid mindId,
        AntagRemovalGrantComponent provenance, AntagGrantRecord grant, out string reason)
    {
        if (grant.GrantedThieving &&
            !provenance.Grants.Values.Any(other => other != grant && other.GrantedThieving))
            RemComp<ThievingComponent>(body);

        if (grant.GrantedPacified &&
            !provenance.Grants.Values.Any(other => other != grant && other.GrantedPacified))
        {
            RemComp<PacifiedComponent>(body);
            provenance.HadPacifiedBeforeCultConversion = false;
        }

        reason = string.Empty;
        return true;
    }
}
