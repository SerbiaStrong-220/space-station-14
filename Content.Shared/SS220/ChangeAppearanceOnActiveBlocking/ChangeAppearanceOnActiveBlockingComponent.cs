using Content.Shared.Hands.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

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

    //[DataField]
    //public ResPath StarsPath = new("Mobs/Effects/stunned.rsiObjects/Weapons/Melee/e_sword_double-inhands.rsi");

    /// <summary>
    /// Layers to add to the sprite of the player that is holding this entity (while the component is toggled on).
    /// </summary>
    //[DataField]
    //public Dictionary<HandLocation, List<PrototypeLayerData>> InhandVisuals = new();
}
