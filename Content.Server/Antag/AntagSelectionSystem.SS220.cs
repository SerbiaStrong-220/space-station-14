using Content.Server.Antag.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Antag;

public sealed partial class AntagSelectionSystem
{
    public bool IsSelectedForMindRole(AntagSelectionComponent selection,
        ICommonSession session,
        EntProtoId mindRole)
    {
        foreach (var definition in selection.Definitions)
        {
            if (definition.MindRoles?.Contains(mindRole) != true ||
                !selection.PreSelectedSessions.TryGetValue(definition, out var sessions))
            {
                continue;
            }

            return sessions.Contains(session);
        }

        return false;
    }
}
