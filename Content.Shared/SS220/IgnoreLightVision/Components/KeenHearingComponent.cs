// EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.IgnoreLightVision;

[RegisterComponent, NetworkedComponent]
public sealed partial class KeenHearingComponent : AddIgnoreLightVisionOverlayComponent
{
    public TimeSpan? ToggleTime;

    public KeenHearingComponent(float radius, float closeRadius) : base(radius, closeRadius) { }
}

[DataDefinition]
public sealed partial class UseKeenHearingEvent : InstantActionEvent
{
    [DataField]
    public float? Duration;
}
