// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.ModuleFurniture.Components;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModuleFurnitureComponent : Component
{
    [AutoNetworkedField]
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

    /// <summary>
    /// In that order we try to fill the TiledItemContainer
    /// </summary>
    [DataField("container")]
    [AutoNetworkedField]
    public Container DrawerContainer = new();

    /// <summary>
    /// Defines layout for containers, first - width, second - height
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public Vector2i TileLayoutSize = new(0, 0);
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

