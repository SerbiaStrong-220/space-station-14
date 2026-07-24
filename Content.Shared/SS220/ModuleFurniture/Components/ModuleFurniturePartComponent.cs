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
    /// <summary>
    /// Describes archetype of part size. Needed for correct placing part's spite in foundation sprite
    /// </summary>
    [DataField("size")]
    [AutoNetworkedField]
    public Vector2i ContainerSize;


    /// <summary>
    /// Size of actually sprite. Starting offset is given by codes
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
