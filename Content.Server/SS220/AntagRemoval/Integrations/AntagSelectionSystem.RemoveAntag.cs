// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Server.Antag.Components;
using Content.Server.SS220.AntagRemoval;
using Content.Shared.Antag;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Antag;

public sealed partial class AntagSelectionSystem
{
    private readonly HashSet<(NetUserId UserId, string MindRole)> _removedRolesThisRound = new();

    private bool IsRoleRevokedForRound(ICommonSession session, AntagSelectionDefinition def)
    {
        return _removedRolesThisRound.Any(removed =>
            removed.UserId == session.UserId && DefinesRole(def, removed.MindRole));
    }

    private static bool DefinesRole(AntagSelectionDefinition def, string roleId)
    {
        if (def.MindRoles?.Any(role => role.Id == roleId) == true)
            return true;

        var antagId = roleId switch
        {
            RemoveAntagRoles.Traitor => "Traitor",
            RemoveAntagRoles.Thief => "Thief",
            RemoveAntagRoles.Cult => "CultistOfYoggSothoth",
            RemoveAntagRoles.InitialInfected => "InitialInfected",
            RemoveAntagRoles.HeadRevolutionary => "HeadRev",
            _ => string.Empty
        };

        return def.PrefRoles.Any(role => role.Id == antagId) ||
               def.FallbackRoles.Any(role => role.Id == antagId);
    }

    /// <summary>Blocks automatic reassignment and releases exactly one original selection slot.</summary>
    public void RevokeAssignment(ICommonSession session, EntityUid mindId, string roleId,
        EntityUid ruleUid, int definitionIndex)
    {
        _removedRolesThisRound.Add((session.UserId, roleId));

        if (TerminatingOrDeleted(ruleUid) ||
            !TryComp<AntagSelectionComponent>(ruleUid, out var selection) ||
            definitionIndex < 0 || definitionIndex >= selection.Definitions.Count)
            return;

        var def = selection.Definitions[definitionIndex];
        if (!DefinesRole(def, roleId) ||
            !selection.PreSelectedSessions.TryGetValue(def, out var selected) ||
            selected.RemoveWhere(selectedSession => selectedSession.UserId == session.UserId) == 0)
            return;

        // A reconnect replaces the session object, but not the account ID.
        var hasOtherAssignment = selection.PreSelectedSessions.Values.Any(sessions =>
            sessions.Any(selectedSession => selectedSession.UserId == session.UserId));

        if (!hasOtherAssignment)
        {
            selection.AssignedSessions.RemoveWhere(assigned => assigned.UserId == session.UserId);
            selection.AssignedMinds.RemoveAll(entry => entry.Item1 == mindId);
        }

        selection.AdminFreedSlots.TryGetValue(definitionIndex, out var freed);
        selection.AdminFreedSlots[definitionIndex] = freed + 1;
    }

    /// <summary>Tries all freed definitions and consumes a slot only after a mind is assigned.</summary>
    private bool TryAssignFreedSlot(Entity<AntagSelectionComponent> rule, ICommonSession session, int players)
    {
        if (!rule.Comp.AssignmentComplete || rule.Comp.SelectionTime == AntagSelectionTime.PrePlayerSpawn)
            return false;

        foreach (var (index, remaining) in rule.Comp.AdminFreedSlots)
        {
            if (remaining <= 0 || index < 0 || index >= rule.Comp.Definitions.Count)
                continue;

            var def = rule.Comp.Definitions[index];
            if (!def.PickPlayer)
                continue;

            var selectedCount = rule.Comp.PreSelectedSessions.TryGetValue(def, out var selected)
                ? selected.Count : 0;

            if (selectedCount >= GetTargetAntagCount(rule, players, def))
                continue;

            var mindCount = rule.Comp.AssignedMinds.Count;
            var wasSelected = selected?.Contains(session) == true;
            var wasAssigned = rule.Comp.AssignedSessions.Contains(session);

            if (!TryMakeAntag(rule, session, def))
                continue;

            // MakeAntag may return early when spawning fails, despite TryMakeAntag returning true.
            if (rule.Comp.AssignedMinds.Count == mindCount)
            {
                if (!wasSelected && rule.Comp.PreSelectedSessions.TryGetValue(def, out selected))
                    selected.Remove(session);

                if (!wasAssigned)
                    rule.Comp.AssignedSessions.Remove(session);

                continue;
            }

            if (remaining == 1)
                rule.Comp.AdminFreedSlots.Remove(index);
            else
                rule.Comp.AdminFreedSlots[index] = remaining - 1;

            return true;
        }

        return false;
    }

    public void ClearAdminFreedSlots(EntityUid ruleUid)
    {
        if (TryComp<AntagSelectionComponent>(ruleUid, out var selection))
            selection.AdminFreedSlots.Clear();
    }

    public void ResetRemovedRolesForNewRound()
    {
        _removedRolesThisRound.Clear();

        var query = EntityQueryEnumerator<AntagSelectionComponent>();
        while (query.MoveNext(out var selection))
            selection.AdminFreedSlots.Clear();
    }
}
