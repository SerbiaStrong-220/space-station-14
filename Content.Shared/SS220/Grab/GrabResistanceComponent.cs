// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Alert;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Grab;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class GrabResistanceComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<GrabStage, float> BaseStageBreakoutChance = new()
    {
        { GrabStage.Passive, 1.0f },
        { GrabStage.Aggressive, 0.2f },
        { GrabStage.NeckGrab, 0.02f },
        { GrabStage.Chokehold, 0.02f }
    };

    [DataField, AutoNetworkedField]
    public Dictionary<GrabStage, float> CurrentStageBreakoutChance = new();

    [DataField, AutoNetworkedField]
    public TimeSpan FirstBreakoutAttemptDelay = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public TimeSpan BreakoutAttemptCooldown = TimeSpan.FromSeconds(30);

    [DataField, AutoNetworkedField]
    public TimeSpan LastBreakoutAttemptAt = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public TimeSpan NextBreakoutAttemptAt = TimeSpan.Zero;

    [DataField]
    public LocId ResistingPopup = "grab-resistance-component-resisting";
}

public sealed partial class GrabBreakoutAttemptAlertEvent : BaseAlertEvent;
