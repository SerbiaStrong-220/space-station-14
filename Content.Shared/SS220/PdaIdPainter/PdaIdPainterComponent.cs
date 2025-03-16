using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.PdaIdPainter;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class PdaIdPainterComponent : Component
{
    public static string IdPainterSlot = "id_painter_slot";
    public static string PdaPainterSlot = "pda_painter_slot";

    [DataField]
    public ItemSlot IdCardSlot = new();

    [DataField]
    public ItemSlot PdaSlot = new();

    [DataField]
    [AutoNetworkedField]
    public EntProtoId? PdaChosenProto;

    [DataField]
    [AutoNetworkedField]
    public EntProtoId? IdChosenProto;

    [DataField]
    [AutoNetworkedField]
    public EntProtoId? PdaDefaultProto;

    [DataField]
    [AutoNetworkedField]
    public EntProtoId? IdDefaultProto;
}
