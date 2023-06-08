using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CryopodSSD;


/// <summary>
/// Component for In-game leaving or AFK
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class CryopodSSDComponent : Component
{
    /// <summary>
    /// List for IC knowing who went in cryo
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public List<string> StoredEntities = new List<string>();

    [DataField("entryDelay")] public float EntryDelay = 6f;

    [DataField("autoTransferToCryoDelay")] public float AutoTransferDelay = 900f;

    [ViewVariables(VVAccess.ReadWrite)] public TimeSpan CurrentEntityLyingInCryopodTime;

    [ViewVariables(VVAccess.ReadWrite)] public ContainerSlot BodyContainer = default!;

    [ViewVariables(VVAccess.ReadWrite)] public ContainerSlot ItemsContainer = default!;

    [Serializable, NetSerializable]
    public enum CryopodSSDVisuals : byte
    {
        ContainsEntity
    }
}