// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Shuttles.BUIStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.WeaponControl;

[Serializable, NetSerializable]

public sealed class WeaponControlBoundUserInterfaceState(NavInterfaceState navState) : BoundUserInterfaceState
{
    public NavInterfaceState NavState = navState;
}
