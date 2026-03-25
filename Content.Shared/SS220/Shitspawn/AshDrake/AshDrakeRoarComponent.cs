// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;

namespace Content.Shared.SS220.Shitspawn.AshDrake;

[RegisterComponent]
public sealed partial class AshDrakeRoarComponent : Component
{
    [DataField]
    public EntityUid? ActionEntity;

    [DataField]
    public float Radius = 8f;

    [DataField]
    public float StunDuration = 1.5f;
}

public sealed partial class AshDrakeRoarActionEvent : InstantActionEvent { }
