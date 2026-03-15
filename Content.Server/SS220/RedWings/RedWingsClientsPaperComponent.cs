using Robust.Shared.Prototypes;

namespace Content.Server.SS220.RedWings;

[RegisterComponent]
public sealed partial class RedWingsClientPaperComponent : Component
{
    private const int DefaultClientAmount = 3;

    [DataField]
    public int ClientAmount = DefaultClientAmount;
}
