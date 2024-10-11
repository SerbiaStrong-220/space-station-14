// Original code github.com/CM-14 Licence MIT, EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.IgnoreLightVision;

[RegisterComponent, NetworkedComponent]
public sealed partial class ThermalVisionComponent : AddIgnoreLightVisionOverlayComponent
{
    public ThermalVisionComponent(float radius, float closeRadius) : base(radius, closeRadius) { }
}

public sealed partial class UseThermalVisionEvent : InstantActionEvent { }
