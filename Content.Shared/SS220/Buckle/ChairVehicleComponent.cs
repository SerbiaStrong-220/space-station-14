// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Buckle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ChairVehcileComponent : Component
{

    /// <summary>
    /// The time to unbuckle disabled
    /// </summary>
    [AutoNetworkedField]

    public float ChairVehicleUnbuckleTime = 2f;

}

