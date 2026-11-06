using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.NightVision;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NightVisionComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled;

    /// <summary> Reusable visual settings, rather than shader parameters copied between entities. </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<NightVisionProfilePrototype> Profile = "NightVisionGreen";

    /// <summary> Higher priority enabled sources take precedence without disabling other sources. </summary>
    [DataField, AutoNetworkedField]
    public int Priority;

    /// <summary>Clothing grants vision only in these slots</summary>
    [DataField]
    public SlotFlags Slots = SlotFlags.EYES;

    /// <summary> Null for passive animal vision. </summary>
    [DataField]
    public EntProtoId? Action = "ActionToggleNightVision";

    [AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary> Wearer or implantee, or the source itself for innate vision. </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Wearer;
}
