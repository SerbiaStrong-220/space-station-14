using Content.Server.GameTicking.Presets;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(SecretRuleSystem))]
public sealed partial class SecretRuleComponent : Component
{
    // SS220-event-director-begin
    /// <summary>
    /// The hidden preset selected for this secret round. Kept so systems that depend on preset
    /// flags (such as random-event disabling) can respect the actual mode.
    /// </summary>
    [DataField("selectedPreset")]
    public ProtoId<GamePresetPrototype>? SelectedPreset;
    // SS220-event-director-end

    /// <summary>
    /// The gamerules that get added by secret.
    /// </summary>
    [DataField("additionalGameRules")]
    public HashSet<EntityUid> AdditionalGameRules = new();
}
