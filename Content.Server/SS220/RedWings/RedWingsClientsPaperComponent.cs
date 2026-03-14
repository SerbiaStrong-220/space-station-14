using Robust.Shared.Prototypes;

namespace Content.Server.SS220.RedWings;

[RegisterComponent]
public sealed partial class RedWingsClientPaperComponent : Component
{
    [DataField]
    public int ClientAmount = 3;
}
