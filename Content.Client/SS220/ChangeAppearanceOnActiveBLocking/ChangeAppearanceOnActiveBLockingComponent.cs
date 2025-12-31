using Content.Shared.Hands.Components;
using Robust.Shared.GameStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.ChangeAppearanceOnActiveBLocking;

[RegisterComponent]
public sealed partial class ChangeAppearanceOnActiveBLockingComponent : Component
{
    [DataField]
    public bool Toggled = false;

    /// <summary>
    /// Sprite layer that will have its visibility toggled when this item is used in active blocking
    /// </summary>
    [DataField(required: true)]
    public string? SpriteLayer;

    /// <summary>
    /// Layers to add to the sprite of the player that is holding this entity (while the component is toggled on).
    /// </summary>
    [DataField]
    public Dictionary<HandLocation, List<PrototypeLayerData>> InhandVisuals = new();
}
