// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.CultYogg.CultMiniMap;

/// <summary>
/// Reusable ordered set of entities and markers displayed by a cult minimap.
/// The first matching rule wins.
/// </summary>
[Prototype]
public sealed partial class CultMiniMapProfilePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public List<ProtoId<CultMiniMapTrackingRulePrototype>> Rules { get; private set; } = new();
}

/// <summary>
/// Selects entities by component and optional prototypes, then describes their marker.
/// </summary>
[DataDefinition]
[Virtual]
public partial class CultMiniMapTrackingRule
{
    /// <summary>
    /// Registered component name, without the Component suffix.
    /// </summary>
    [DataField("component", required: true, customTypeSerializer: typeof(ComponentNameSerializer))]
    public string ComponentName = string.Empty;

    /// <summary>
    /// Optional entity prototype filter. An empty list matches every entity with <see cref="ComponentName"/>.
    /// </summary>
    [DataField]
    public List<EntProtoId> Prototypes = new();

    /// <summary>
    /// Optional localization key for the type shown in the list. Defaults to the component name.
    /// </summary>
    [DataField]
    public LocId? Label;

    /// <summary>
    /// PNG texture or RSI state. The map displays its first frame.
    /// </summary>
    [DataField]
    public SpriteSpecifier Icon =
        new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/NavMap/beveled_circle.png"));

    [DataField]
    public Color Color = Color.White;

    /// <summary>
    /// Positive size multiplier for the map marker. List icons have a fixed size.
    /// </summary>
    [DataField]
    public float Scale = 1f;

    /// <summary>
    /// How the marker is drawn. Structural marker types use map-scaled vector geometry instead of <see cref="Icon"/>.
    /// </summary>
    [DataField]
    public CultMiniMapMarkerType MarkerType = CultMiniMapMarkerType.Icon;

    /// <summary>
    /// Whether matching entities appear in the side list. They remain visible on the map when disabled.
    /// </summary>
    [DataField]
    public bool ShowInList = true;

    /// <summary>
    /// Whether health is collected and shown for matching entities.
    /// </summary>
    [DataField]
    public bool ShowHealth = true;
}

/// <summary>
/// Named, reusable tracking rule. Supports prototype inheritance so related markers
/// can share their component, color and visibility settings.
/// </summary>
[Prototype]
public sealed partial class CultMiniMapTrackingRulePrototype : CultMiniMapTrackingRule,
    IPrototype,
    IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<CultMiniMapTrackingRulePrototype>))]
    public string[]? Parents { get; private set; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }
}
