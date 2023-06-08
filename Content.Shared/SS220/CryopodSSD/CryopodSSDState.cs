using System.Linq;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CryopodSSD;

[Serializable, NetSerializable]
public enum CryopodSSDKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class CryopodSSDState : BoundUserInterfaceState
{
    public List<string> CryopodSSDRecords { get; }

    public CryopodSSDState(List<string> cryopodSSDRecords)
    {
        CryopodSSDRecords = cryopodSSDRecords;
    }
}