using Content.Shared.Humanoid;

namespace Content.Server.SS220.PenScrambler;

[RegisterComponent]
public sealed partial class PenScramblerComponent : Component
{
    [DataField]
    public EntityUid? NullspaceClone;

    [DataField]
    public bool HaveDna = false;

    public TimeSpan DelayForExtractDna = TimeSpan.FromSeconds(5);
    public TimeSpan DelayForTransferToImplant = TimeSpan.FromSeconds(3);
}
