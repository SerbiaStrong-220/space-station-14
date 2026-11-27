// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.SS220.GameTicking.Rules;
using Content.Shared.CombatMode.Pacification;

namespace Content.Server.SS220.AntagRemoval.Handlers;

public sealed class CultRemovalSystem : EntitySystem, IAntagRemovalHandler
{
    [Dependency] private CultYoggRuleSystem _cult = default!;

    public bool RemovesMindRole => true;

    public bool CanRemove(EntityUid body, EntityUid mindId, out string reason)
        => _cult.CanRemoveCultistRoleByAdmin(body, out reason);

    public bool TryRemove(EntityUid body, EntityUid mindId,
        AntagRemovalGrantComponent provenance, AntagGrantRecord grant, out string reason)
    {
        if (!_cult.TryRemoveCultistRoleByAdmin(body, out reason))
            return false;

        if (provenance.HadPacifiedBeforeCultConversion)
            EnsureComp<PacifiedComponent>(body);

        return true;
    }
}
