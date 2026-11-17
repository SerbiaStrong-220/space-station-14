// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Linq;
using Content.Server.Antag.Components;
using Content.Shared.Antag;
using Content.Shared.Mind;
using Robust.Shared.Player;

using Content.Server.SS220.AntagRemoval;

namespace Content.Server.Antag;

public sealed partial class AntagSelectionSystem
{
    private readonly HashSet<(string UserId, string MindRole)> _removedRolesThisRound = new();
    // Store original definition indices so a freed slot cannot become another role.
    private readonly Dictionary<EntityUid, HashSet<int>> _adminFreedDefinitions = new();

    private bool IsRoleRevokedForRound(ICommonSession session, AntagSelectionDefinition def)
    {
        return _removedRolesThisRound.Any(removed =>
            removed.UserId == session.UserId.ToString() && DefinesRole(def, removed.MindRole));
    }

    private static bool DefinesRole(AntagSelectionDefinition def, string roleId)
    {
        if (def.MindRoles?.Any(r => r.Id == roleId) == true)
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
        return def.PrefRoles.Any(r => r.Id == antagId) ||
               def.FallbackRoles.Any(r => r.Id == antagId);
    }

    /// <summary>Blocks automatic reassignment and releases the original selection slot.</summary>
    public void RevokeAssignment(ICommonSession session, EntityUid mindId, string roleId,
        EntityUid ruleUid, int definitionIndex)
    {
        _removedRolesThisRound.Add((session.UserId.ToString(), roleId));

        if (TerminatingOrDeleted(ruleUid) ||
            !TryComp<AntagSelectionComponent>(ruleUid, out var selection) ||
            definitionIndex < 0 || definitionIndex >= selection.Definitions.Count)
            return;

        var def = selection.Definitions[definitionIndex];
        if (!DefinesRole(def, roleId) ||
            !selection.PreSelectedSessions.TryGetValue(def, out var selected) ||
            !selected.Remove(session))
            return;

        // Preserve the active count of other roles and other definitions.
        if (!selection.PreSelectedSessions.Values.Any(sessions => sessions.Contains(session)))
        {
            selection.AssignedSessions.Remove(session);
            selection.AssignedMinds.RemoveAll(entry => entry.Item1 == mindId);
        }

        if (!_adminFreedDefinitions.TryGetValue(ruleUid, out var freed))
            _adminFreedDefinitions[ruleUid] = freed = new HashSet<int>();
        freed.Add(definitionIndex);
    }

    /// <summary>Finds an unfilled original definition for late join.</summary>
    private bool TryGetFreedDefinition(Entity<AntagSelectionComponent> rule, int players,
        out AntagSelectionDefinition? found)
    {
        found = null;
        if (!rule.Comp.AssignmentComplete ||
            rule.Comp.SelectionTime != AntagSelectionTime.PostPlayerSpawn ||
            !_adminFreedDefinitions.TryGetValue(rule.Owner, out var indices))
            return false;

        foreach (var index in indices.OrderBy(i => i))
        {
            if (index < 0 || index >= rule.Comp.Definitions.Count)
                continue;
            var def = rule.Comp.Definitions[index];
            if (!def.PickPlayer)
                continue;
            var selectedCount = rule.Comp.PreSelectedSessions.TryGetValue(def, out var sessions)
                ? sessions.Count : 0;
            if (selectedCount >= GetTargetAntagCount(rule, players, def))
                continue;
            found = def;
            return true;
        }
        return false;
    }

    public void ResetRemovedRolesForNewRound()
    {
        _removedRolesThisRound.Clear();
        _adminFreedDefinitions.Clear();
    }
}
