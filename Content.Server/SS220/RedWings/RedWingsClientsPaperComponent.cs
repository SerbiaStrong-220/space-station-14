using Robust.Shared.Prototypes;

namespace Content.Server.SS220.RedWings;

[RegisterComponent]
public sealed partial class RedWingsClientPaperComponent : Component
{
    private const int DefaultClientAmount = 3;
    private static readonly string[] DefaultForbiddenDepartment = ["Command", "Security"];

    [DataField]
    public int ClientAmount = DefaultClientAmount;

    [DataField]
    public string[] ForbiddenDepartment = DefaultForbiddenDepartment;
}
