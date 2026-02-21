using Robust.Shared.GameStates;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.SS220.Deafenation;

/// <summary>
/// Will cause a deafening effect in an area around the entity when triggered.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DeafeningOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The range in which to deafen entities.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 1.0f;

    /// <summary>
    /// The probability to apply the deafening effect.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Probability = 1.0f;

    [DataField, AutoNetworkedField]
    public float KnockdownTime = 1.0f;

    [DataField, AutoNetworkedField]
    public float StunTime = 1.0f;
}
