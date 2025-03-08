// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Tools;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.ModuleFurniture.Components;

public abstract partial class SharedModuleFurnitureComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public SortedDictionary<Vector2i, NetEntity> CachedLayout = new(
        Comparer<Vector2i>.Create(
            TiledLayoutHelpers.TiledLayoutVector2iComparer
        )
    );

    public SortedDictionary<Vector2i, bool> CachedOccupation = new(
        Comparer<Vector2i>.Create(
            TiledLayoutHelpers.TiledLayoutVector2iComparer
        )
    );

    public static string ContainerId = "module-furniture";

    /// <summary>
    /// The tool quality needed to get the container content out
    /// </summary>
    [DataField]
    public ProtoId<ToolQualityPrototype> RemovingTool = "Prying";

    [DataField]
    public float RemovingDelaySeconds = 2f;

    /// <summary>
    /// The tool quality needed to get the container content out
    /// </summary>
    [DataField]
    public ProtoId<ToolQualityPrototype> DeconstructTool = "Prying";

    [DataField]
    public float DeconstructDelaySeconds = 2f;

    /// <summary>
    /// Defines layout for containers, first - width, second - height
    /// </summary>
    [DataField]
    public Vector2i TileLayoutSize = new(0, 0);

    /// <summary>
    /// Defines pixel per layout tile. Part width will be compared with it.
    /// </summary>
    [DataField(required: true)]
    [ViewVariables(VVAccess.ReadOnly)]
    public Vector2i PixelPerLayoutTile;

    [DataField]
    public float PixelPerMeter = 32f;
}

[Serializable, NetSerializable]
public sealed class ModuleFurnitureComponentState : ComponentState
{
    public List<bool> Occupation { get; }
    public Dictionary<Vector2i, NetEntity> Layout { get; }
    public Vector2i TileLayoutSize { get; }

    public ModuleFurnitureComponentState(List<bool> occupation, Dictionary<Vector2i, NetEntity> layout, Vector2i tileLayoutSize) : base()
    {
        Occupation = occupation;
        Layout = layout;
        TileLayoutSize = tileLayoutSize;
    }
}

/// <summary>
/// This helps to correspond client sprite size and server tiled layout of itemContainers.
/// </summary>
public enum ContainerTileSize : int
{
    invalid = -1,
    w1h1 = 1,
    w2h1 = 2,
    w3h1 = 3,
}
