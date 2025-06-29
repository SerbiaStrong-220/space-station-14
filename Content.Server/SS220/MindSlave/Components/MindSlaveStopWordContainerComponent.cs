// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Server.SS220.MindSlave.Components;

[RegisterComponent]
public sealed partial class MindSlaveStopWordContainerComponent : Component
{
    // to pass tests add values
    [DataField]
    public string Collection = "nanotrasen_central_command";
    [DataField]
    public string Group = "roundstart";
    [DataField]
    public string Form = "hos_mindslave_briefing";

    /// <summary>
    /// This stamp will be applied to list
    /// </summary>
    [DataField]
    public List<EntProtoId> StampList = new();
}
