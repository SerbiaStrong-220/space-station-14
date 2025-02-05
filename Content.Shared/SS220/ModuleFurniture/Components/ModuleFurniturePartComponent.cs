// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.ModuleFurniture.Components;

/// <summary>
/// Defines if it can be inserted into TiledItemContainers
/// </summary>
[RegisterComponent]
public sealed partial class ModuleFurniturePartComponent : Component
{
    [DataField("size")]
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
}
