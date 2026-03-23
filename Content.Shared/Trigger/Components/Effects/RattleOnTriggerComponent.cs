using Content.Shared.Mobs;
using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Sends an emergency message over coms when triggered giving information about the entity's mob status.
/// If TargetUser is true then the user's mob state will be used instead.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RattleOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The radio channel the message will be sent to.
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> RadioChannel = "Syndicate";

    // SS220 - death-rattle-implant - BGN
    /// <summary>
    /// Optional list of channels that can be selected in UI before injection.
    /// If empty, current <see cref="RadioChannel"/> is used as the only option.
    /// </summary>
    [DataField]
    public List<ProtoId<RadioChannelPrototype>> PossibleChannels = new();

    /// <summary>
    /// Explicitly enabled channels for this implant. If empty, falls back to <see cref="RadioChannel"/>.
    /// </summary>
    [DataField]
    public List<ProtoId<RadioChannelPrototype>> EnabledChannels = new();
    // SS220 - death-rattle-implant - END

    /// <summary>
    /// The message to be send depending on the target's current mob state.
    /// </summary>
    [DataField]
    public Dictionary<MobState, LocId> Messages = new()
    {
        {MobState.Critical, "rattle-on-trigger-critical-message"},
        {MobState.Dead, "rattle-on-trigger-dead-message"}
    };
}
