namespace Content.Server.SS220.EvilTwin;

[RegisterComponent]
public sealed partial class EvilTwinSpawnerComponent : Component
{
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("target")]
        public EntityUid TargetForce { get; set; } = EntityUid.Invalid;
}
