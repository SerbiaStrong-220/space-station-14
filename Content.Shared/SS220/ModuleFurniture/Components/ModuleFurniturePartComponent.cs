// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.ModuleFurniture.Components;

/// <summary>
/// Defines if it can be inserted into TiledItemContainers
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModuleFurniturePartComponent : Component
{
    [DataField("size")]
    [AutoNetworkedField]
    public ContainerTileSize ContainerSizeType;

    public Vector2i ContainerSize
    {
        get
        {
            return ContainerSizeType switch
            {
                ContainerTileSize.w1h1 => new Vector2i(1, 1),
                ContainerTileSize.w2h1 => new Vector2i(2, 1),
                ContainerTileSize.w3h1 => new Vector2i(3, 1),
                _ => throw new Exception("Container size out of range or invalid")
            };
        }
    }

    /// <summary>
    /// Size of actually sprite. Starting offset is given
    /// </summary>
    [DataField(required: true)]
    public Vector2i SpriteSize;

    /// <summary>
    /// Used to correctly change draw depth in client code.
    /// </summary>
    public bool Opened;
}

[Serializable, NetSerializable]
public enum ModuleFurniturePartVisuals : byte
{
    InFurniture,
    Opened,
    LayerOpened,
    LayerClosed,
    LayerItem
}
