// EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.IgnoreLightVision.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class KeenHearingComponent : AddIgnoreLightVisionOverlayComponent
{
    public TimeSpan? ToggleTime;

    [ViewVariables]
    public bool ManualOn;

    public KeenHearingComponent(float radius, float closeRadius) : base(radius, closeRadius) { }
}

[ByRefEvent]
public record struct GetKeenHearingModifiersEvent
{
    public bool ForceOn;
}

[DataDefinition]
public sealed partial class UseKeenHearingEvent : InstantActionEvent
{
    [DataField]
    public float? Duration;
}
