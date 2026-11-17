// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.GameTicking.Rules.Components;

namespace Content.Server.GameTicking.Rules;

public sealed partial class TraitorRuleSystem
{
    /// <summary>Only remove the named mind from the live traitor count.</summary>
    public void RemoveTraitorFromRoundAccounting(EntityUid mindId)
    {
        var query = EntityQueryEnumerator<TraitorRuleComponent>();
        while (query.MoveNext(out _, out var rule))
            rule.TraitorMinds.Remove(mindId);
    }
}
