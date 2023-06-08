using System.Linq;
using Content.Shared.Storage;
using Robust.Shared.Serialization;
using static Content.Shared.Storage.SharedStorageComponent;
namespace Content.Shared.SS220.CryopodSSD;

[Serializable, NetSerializable]
public enum CryopodSSDKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class CryopodSSDState : BoundUserInterfaceState
{
    public bool HasAccess { get; }
    public StorageBoundUserInterfaceState StorageState { get; }
    public List<string> CryopodSSDRecords { get; }

    public CryopodSSDState(bool hasAccess, List<string> cryopodSSDRecords, StorageBoundUserInterfaceState storageState)
    {
        HasAccess = hasAccess;
        CryopodSSDRecords = cryopodSSDRecords;
        StorageState = storageState;
    }
}