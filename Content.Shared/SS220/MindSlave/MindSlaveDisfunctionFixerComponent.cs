// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.MindSlave;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class MindSlaveDisfunctionFixerComponent : Component
{
    /// <summary>
    /// How much disfunction components will be removed. Can be allied on each stage of disfunction.
    /// </summary>
    [DataField("remove")]
    [AutoNetworkedField]
    public int RemoveAmount = 2;

    /// <summary>
    /// How much will be added until disfunction progress. Can be allied on each stage of disfunction.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public float DelayMinutes = 5;
}
