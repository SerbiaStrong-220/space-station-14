// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.CultYogg.CultMiniMap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class CultMiniMapComponent : Component
{
    public override bool SendOnlyToOwner => true;

    /// <summary>
    /// Private snapshot for the owner's open map. Never store it in the public UI state.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public CultMiniMapState? State;

    /// <summary>
    /// Marker used only for this map's owner. The owner is always shown in a separate section.
    /// </summary>
    [DataField]
    public SpriteSpecifier SelfIcon =
        new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/NavMap/beveled_star.png"));

    /// <summary>
    /// Color of the owner marker.
    /// </summary>
    [DataField]
    public Color SelfColor = Color.Cyan;

    /// <summary>
    /// Positive size multiplier for the owner marker.
    /// </summary>
    [DataField]
    public float SelfScale = 1.2f;

    /// <summary>
    /// Only map owners with the same channel receive each other's pings.
    /// </summary>
    [DataField]
    public string PingChannel = "cult-yogg";

    /// <summary>
    /// Texture or RSI state used for channel pings.
    /// </summary>
    [DataField]
    public SpriteSpecifier PingIcon =
        new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/NavMap/beveled_circle.png"));

    /// <summary>
    /// Color of channel pings created by this owner.
    /// </summary>
    [DataField]
    public Color PingColor = Color.DeepSkyBlue;

    /// <summary>
    /// Positive size multiplier for channel pings.
    /// </summary>
    [DataField]
    public float PingScale = 1.2f;

    /// <summary>
    /// How long a ping remains visible. YAML values are in seconds.
    /// </summary>
    [DataField]
    public TimeSpan PingDuration = TimeSpan.FromSeconds(8);

    /// <summary>
    /// Minimum interval between this owner's pings. YAML values are in seconds.
    /// </summary>
    [DataField]
    public TimeSpan PingCooldown = TimeSpan.FromSeconds(3);

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
