using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Telepathy;

/// <summary>
/// This is used for giving telepathy ability
/// </summary>
[RegisterComponent]
public sealed partial class TelepathyComponent : Component
{
    [DataField]
    public EntProtoId Action = "TelepathyAction";

    [DataField]
    public EntityUid? ActionEntity;
}
