// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Hands.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.ChangeAppearanceOnActiveBlocking;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ChangeAppearanceOnActiveBlockingComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Toggled = false;

    /// <summary>
    /// Sprite layer that will have its visibility toggled when this item is toggled.
    /// </summary>
    [DataField(required: true)]
    public string? SpriteLayer;

    /// <summary>
    /// Layers to add to the sprite of the player that is holding this entity (while the component is toggled on).
    /// </summary>
    [DataField]
    public Dictionary<HandLocation, List<PrototypeLayerData>> InhandVisuals = new();

}
