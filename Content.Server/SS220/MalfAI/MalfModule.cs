// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Mind;
using Content.Shared.Roles.Components;
using Content.Shared.SS220.MalfAI;
using Content.Shared.Store;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.MalfAI;

public readonly record struct MalfExecutionContext(
    EntityUid Performer,
    EntityUid MindId,
    Entity<MindRoleComponent, MalfAiRoleComponent> Role,
    EntityUid Station);

[ByRefEvent]
public record struct MalfModuleExecuteEvent(MalfExecutionContext Context, ProtoId<ListingPrototype> ListingId)
{
    public bool Handled;
}
