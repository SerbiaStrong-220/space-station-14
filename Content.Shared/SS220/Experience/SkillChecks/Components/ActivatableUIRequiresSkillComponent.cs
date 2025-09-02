// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience.SkillChecks.Components;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ActivatableUIRequiresSkillComponent : Component
{
    [DataField(required: true)]
    [AutoNetworkedField]
    public ProtoId<SkillTreePrototype> TreeProtoId;

    [DataField(required: true)]
    [AutoNetworkedField]
    public ProtoId<SkillPrototype> SkillProtoId;

    [DataField]
    public LocId? PopupMessage = "skill-check-bui-not-met";
}
