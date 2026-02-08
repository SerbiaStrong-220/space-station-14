// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Hands.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Mech.Parts.Components;

/// <summary>
/// Arm of the mech
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MechArmComponent : Component
{
    /// <summary>
    /// The hands that are provided.
    /// </summary>
    [DataField(required: true)]
    public List<MechHand> Hands = new();

    /// <summary>
    /// The items stored within the hands. Null until the first time items are stored.
    /// </summary>
    [DataField]
    public Dictionary<string, EntityUid>? StoredItems;

    /// <summary>
    /// An ID for the container where items are stored when not in use.
    /// </summary>
    [DataField]
    public string HoldingContainer = "holding_container";
}

[DataDefinition, Serializable, NetSerializable]
public partial record struct MechHand
{
    [DataField]
    public EntProtoId? Item = null;

    [DataField]
    public Hand Hand = new();

    [DataField]
    public bool ForceRemovable = false;

    public MechHand( Hand hand, bool forceRemovable = false,EntProtoId ? item = null)
    {
        Item = item;
        Hand = hand;
        ForceRemovable = forceRemovable;
    }
}
