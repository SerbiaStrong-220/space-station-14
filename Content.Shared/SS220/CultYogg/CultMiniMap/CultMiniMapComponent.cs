// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.CultYogg.CultMiniMap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CultMiniMapComponent : Component
{
    /// <summary>
    /// Marker used only for this map's owner. The owner is always shown in a separate section.
    /// </summary>
    [DataField]
    public SpriteSpecifier SelfIcon =
        new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/NavMap/beveled_star.png"));

    [DataField]
    public Color SelfColor = Color.Cyan;

    [DataField]
    public float SelfScale = 1.2f;

    /// <summary>
    /// Only map owners with the same channel receive each other's pings.
    /// </summary>
    [DataField]
    public string PingChannel = "cult-yogg";

    [DataField]
    public SpriteSpecifier PingIcon =
        new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/NavMap/beveled_circle.png"));

    [DataField]
    public Color PingColor = Color.DeepSkyBlue;

    [DataField]
    public float PingScale = 1.2f;

    [DataField]
    public float PingDuration = 8f;

    [DataField]
    public float PingCooldown = 3f;

    /// <summary>
    /// Number of the latest channel pings shown on this owner's map.
    /// </summary>
    [DataField]
    public int MaxActivePings = 8;

    /// <summary>
    /// Reusable ordered set of tracking rules.
    /// </summary>
    [DataField]
    public ProtoId<CultMiniMapProfilePrototype> TrackingProfile = "CultYogg";

    /// <summary>
    /// Optional owner-specific replacement for <see cref="TrackingProfile"/> rules.
    /// An empty list displays only the map owner.
    /// </summary>
    [DataField]
    public List<CultMiniMapTrackingRule>? TrackingRules;

    [ViewVariables]
    public EntProtoId MiniMapAction = "ActionCultMiniMap";

    [ViewVariables, AutoNetworkedField]
    public EntityUid? MiniMapActionEntity;
}

public sealed partial class CultMiniMapActionEvent : InstantActionEvent
{
}
