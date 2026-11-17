// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Mind;
using Content.Shared.Revolutionary.Components;

namespace Content.Server.SS220.AntagRemoval.Handlers;

public sealed class HeadRevolutionaryRemovalSystem : EntitySystem, IAntagRemovalHandler
{
    public bool RemovesMindRole => false;

    public bool CanRemove(EntityUid body, EntityUid mindId, out string reason)
    {
        var valid = HasComp<HeadRevolutionaryComponent>(body);
        reason = valid ? string.Empty : Loc.GetString("ss220-removeantag-error-unavailable");
        return valid;
    }

    public bool TryRemove(EntityUid body, EntityUid mindId,
        AntagRemovalGrantComponent provenance, AntagGrantRecord grant, out string reason)
    {
        RemComp<HeadRevolutionaryComponent>(body);
        if (!HasRevolutionaryMindRole(mindId))
            RemComp<RevolutionaryComponent>(body);
        reason = string.Empty;
        return true;
    }

    private bool HasRevolutionaryMindRole(EntityUid mindId)
    {
        if (!TryComp<MindComponent>(mindId, out var mind))
            return false;
        foreach (var role in mind.MindRoleContainer.ContainedEntities)
            if (MetaData(role).EntityPrototype?.ID == RemoveAntagRoles.Revolutionary)
                return true;
        return false;
    }
}
