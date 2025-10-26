namespace Content.Shared.SS220.GhostExtension;

[RegisterComponent]
public sealed partial class GhostExtensionComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<EntityUid> Trail;
}
