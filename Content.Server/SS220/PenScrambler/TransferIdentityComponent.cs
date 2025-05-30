using Content.Shared.Humanoid;

namespace Content.Server.SS220.PenScrambler;

[RegisterComponent]
public sealed partial class TransferIdentityComponent : Component
{
    [DataField]
    public EntityUid? NullspaceClone;
}
