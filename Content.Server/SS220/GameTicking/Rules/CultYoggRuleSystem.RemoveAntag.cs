// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Shared.Roles.Components;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.SS220.CultYogg.MiGo;
using Content.Shared.SS220.Roles;

namespace Content.Server.SS220.GameTicking.Rules;


public sealed partial class CultYoggRuleSystem
{
    public bool CanRemoveCultistRoleByAdmin(EntityUid body, out string reason)
    {
        reason = Loc.GetString("ss220-removeantag-error-unavailable");
        if (TerminatingOrDeleted(body) || HasComp<MiGoComponent>(body) ||
            HasComp<AscendingComponent>(body) || !HasComp<CultYoggComponent>(body) ||
            !TryGetCultGameRule(out _))
            return false;

        if (!_mind.TryGetMind(body, out var mindId, out var mind) ||
            mind.OwnedEntity != body ||
            !_role.MindHasRole<CultYoggRoleComponent>(mindId, out _) ||
            !mind.MindRoleContainer.ContainedEntities.Any(role => HasComp<JobRoleComponent>(role)))
            return false;

        reason = string.Empty;
        return true;
    }

    public bool TryRemoveCultistRoleByAdmin(EntityUid body, out string reason)
    {
        if (!CanRemoveCultistRoleByAdmin(body, out reason))
            return false;

        var ev = new CultYoggDeCultingEvent(body);
        RaiseLocalEvent(body, ref ev, true);

        if (HasComp<CultYoggComponent>(body) ||
            (_mind.TryGetMind(body, out var mindId, out _) &&
             _role.MindHasRole<CultYoggRoleComponent>(mindId, out _)))
        {
            reason = Loc.GetString("ss220-removeantag-error-incomplete");
            return false;
        }

        reason = string.Empty;
        return true;
    }
}
