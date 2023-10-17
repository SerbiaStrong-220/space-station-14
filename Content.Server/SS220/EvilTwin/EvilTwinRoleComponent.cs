using Content.Shared.Mind;
using Content.Shared.Roles;

namespace Content.Server.SS220.EvilTwin;

[RegisterComponent]
public sealed partial class EvilTwinRoleComponent : AntagonistRoleComponent
{
    public EntityUid? Target { get; set; }
    public EntityUid? TargetMindId { get; set; }
    public MindComponent? TargetMind { get; set; }
}
