// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Content.Shared.Zombies;

namespace Content.Server.SS220.AntagRemoval.Handlers;

public sealed class InitialInfectedRemovalSystem : EntitySystem, IAntagRemovalHandler
{
    [Dependency] private SharedActionsSystem _actions = default!;

    public bool RemovesMindRole => false;

    public bool CanRemove(EntityUid body, EntityUid mindId, out string reason)
    {
        reason = string.Empty;
        return true;
    }

    public bool TryRemove(EntityUid body, EntityUid mindId,
        AntagRemovalGrantComponent provenance, AntagGrantRecord grant, out string reason)
    {
        if (grant.GrantedIncurableZombie &&
            TryComp<IncurableZombieComponent>(body, out var infection))
        {
            if (infection.Action is { } action)
                _actions.RemoveAction(body, action);
            RemComp<IncurableZombieComponent>(body);
        }
        if (grant.GrantedPatientZero)
            RemComp<InitialInfectedComponent>(body);
        if (grant.GrantedZombifyOnDeath)
            RemComp<ZombifyOnDeathComponent>(body);
        if (grant.GrantedPendingZombie)
            RemComp<PendingZombieComponent>(body);

        reason = string.Empty;
        return true;
    }
}
