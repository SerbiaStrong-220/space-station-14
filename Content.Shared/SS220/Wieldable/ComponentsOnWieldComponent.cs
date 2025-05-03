using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Wieldable.Components;

namespace Content.Shared.SS220.Wieldable;

/// <summary>
/// Adds components on wielded and remove them when unwielded.
/// Requires <see cref="WieldableComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ComponentsOnWieldComponent : Component
{
    /// <summary>
    /// The components to add when wielded.
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    /// <summary>
    /// The components to remove when unwielded.
    /// If this is null <see cref="Components"/> is reused.
    /// </summary>
    [DataField]
    public ComponentRegistry? RemoveComponents;
}
