// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;
namespace Content.Shared.NPC.Systems;
public sealed partial class NpcFactionSystem
{
    /// <summary>Read-only snapshot for role-source bookkeeping.</summary>
    public HashSet<ProtoId<NpcFactionPrototype>> SnapshotFactions(EntityUid uid)
    {
        return TryComp<NpcFactionMemberComponent>(uid, out var member)
            ? new HashSet<ProtoId<NpcFactionPrototype>>(member.Factions)
            : new HashSet<ProtoId<NpcFactionPrototype>>();
    }
}
