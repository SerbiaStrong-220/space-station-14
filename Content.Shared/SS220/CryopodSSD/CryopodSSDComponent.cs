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

    /// <summary>
    /// Delay before climbing in cryopod
    /// </summary>
    [DataField("entryDelay")] public float EntryDelay = 6f;
    
    /// <summary>
    /// Time to afk before automatic cryostorage transfer
    /// </summary>
    [DataField("autoTransferToCryoDelay")] public float AutoTransferDelay = 900f;

    [ViewVariables(VVAccess.ReadWrite)] public TimeSpan CurrentEntityLyingInCryopodTime;

    [ViewVariables(VVAccess.ReadWrite)] public ContainerSlot BodyContainer = default!;

    [Serializable, NetSerializable]
    public enum CryopodSSDVisuals : byte
    {
        ContainsEntity
    }
}