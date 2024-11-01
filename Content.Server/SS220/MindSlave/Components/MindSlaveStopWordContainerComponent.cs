// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Server.SS220.MindSlave.Components;

[RegisterComponent]
public sealed partial class MindSlaveStopWordContainerComponent : Component
{
    [DataField]
    public string Collection = "";
    [DataField]
    public string Group = "";
    [DataField]
    public string Form = "";

    /// <summary>
    /// This stamp will be applied to list
    /// </summary>
    [DataField]
    public List<EntProtoId> StampList = new();
}
