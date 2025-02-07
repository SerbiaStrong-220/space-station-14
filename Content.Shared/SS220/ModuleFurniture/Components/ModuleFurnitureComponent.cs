// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

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

    [DataField]
    public string ContainerId = "module-furniture";

    /// <summary>
    /// Defines layout for containers, first - width, second - height
    /// </summary>
    [DataField]
    public Vector2i TileLayoutSize = new(0, 0);
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

