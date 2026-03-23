using System.Linq;
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
    public HashSet<ProtoId<RadioChannelPrototype>> PossibleChannels = new();

    /// <summary>
    /// Explicitly enabled channels for this implant (user selection via UI).
    /// Always falls back to <see cref="RadioChannel"/> when empty — both for legacy implants and
    /// configurable ones where the user has not yet made a selection or disabled everything.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<RadioChannelPrototype>> EnabledChannels = new();

    /// <summary>
    /// Returns channels available for selection in the UI.
    /// Falls back to <see cref="RadioChannel"/> when <see cref="PossibleChannels"/> is empty (legacy implants).
    /// </summary>
    public IEnumerable<ProtoId<RadioChannelPrototype>> AllowedChannels =>
        PossibleChannels.Count == 0 ? new[] { RadioChannel } : PossibleChannels;

    /// <summary>
    /// Returns channels that are currently active for broadcasting.
    /// When <see cref="EnabledChannels"/> is empty (no explicit selection), falls back to <see cref="RadioChannel"/>
    /// so that the implant always broadcasts to at least one channel.
    /// </summary>
    public IEnumerable<ProtoId<RadioChannelPrototype>> ActiveChannels =>
        EnabledChannels.Count > 0 ? EnabledChannels : new[] { RadioChannel };

    /// <summary>
    /// Keeps <see cref="RadioChannel"/> in sync with the lexicographically first enabled channel.
    /// This ensures <see cref="RadioChannel"/> stays valid for any legacy code that reads it directly.
    /// </summary>
    public void SyncPrimaryChannel()
    {
        if (EnabledChannels.Count > 0)
            RadioChannel = EnabledChannels.First();
    }
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
