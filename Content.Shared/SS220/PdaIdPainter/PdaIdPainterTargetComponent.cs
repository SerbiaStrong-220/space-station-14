using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.PdaIdPainter;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class PdaIdPainterTargetComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public EntProtoId? NewProto;
}
