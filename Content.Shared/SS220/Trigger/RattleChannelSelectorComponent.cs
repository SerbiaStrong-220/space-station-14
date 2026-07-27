// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Radio;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Trigger;

/// <summary>
/// Lets the channel of a <see cref="RattleOnTriggerComponent"/> be picked from a fixed list while the
/// implant is still sitting inside an implanter, i.e. before it gets injected into anyone.
/// The pick is written straight into <see cref="RattleOnTriggerComponent.RadioChannel"/>, so there is
/// no second copy of the selection to keep in sync.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RattleChannelSelectorComponent : Component
{
    /// <summary>
    /// The channels that may be picked.
    /// Include the channel the implant starts on, otherwise it can be switched away from but never back.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public HashSet<ProtoId<RadioChannelPrototype>> Channels = new();
}
