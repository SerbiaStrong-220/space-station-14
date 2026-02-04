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
    public TimeSpan BreakoutAttemptCooldown = TimeSpan.FromSeconds(30);

    [DataField, AutoNetworkedField]
    public TimeSpan LastBreakoutAttemptAt = TimeSpan.Zero;
}
