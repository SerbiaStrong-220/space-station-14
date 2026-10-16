// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Ghost;

/// <summary>
/// Available HUDs in display order. Enabled starts with the prototype default and tracks the player's selection.
/// </summary>
[RegisterComponent]
public sealed partial class GhostHudSettingsComponent : Component
{
    [DataField(required: true)]
    public List<GhostHudSetting> Huds = [];
}

[DataDefinition]
public sealed partial class GhostHudSetting
{
    [DataField(required: true)]
    public ProtoId<GhostHudPrototype> Id;

    [DataField]
    public bool Enabled;
}
