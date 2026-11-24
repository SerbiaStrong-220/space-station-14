// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;

namespace Content.Server.GameTicking.Rules;

public sealed partial class RevolutionaryRuleSystem
{
    /// <summary>Uses the normal defeat/deconversion logic after administrative removal is complete.</summary>
    public void RecheckAfterHeadRemoval(EntityUid ruleUid)
    {
        if (!HasComp<RevolutionaryRuleComponent>(ruleUid) || !HasComp<ActiveGameRuleComponent>(ruleUid))
            return;

        if (CheckRevsLose())
            _antag.ClearAdminFreedSlots(ruleUid);
    }
}
