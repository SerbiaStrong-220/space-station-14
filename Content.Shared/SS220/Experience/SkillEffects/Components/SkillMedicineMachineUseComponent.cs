// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Experience.SkillEffects.Components;

/// <summary>
/// This is used to stop entity from being disarmed
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class SkillMedicineMachineUseComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public float HealthAnalyzerInfoShuffleChance = 0f;

    [DataField]
    [AutoNetworkedField]
    public float DefibrillatorSelfDamageChance = 0f;

    [DataField]
    [AutoNetworkedField]
    public float DefibrillatorFailureChance = 0f;
}

[ByRefEvent]
public record struct GetHealthAnalyzerShuffleChance()
{
    public float ShuffleChance = 0f;
}

[ByRefEvent]
public record struct GetDefibrillatorUseChances()
{
    public float SelfDamageChance = 0f;
    public float FailureChance = 0f;
}
