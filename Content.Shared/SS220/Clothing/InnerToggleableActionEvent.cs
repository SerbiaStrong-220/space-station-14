using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Clothing;

/// <summary>
/// Generic action-event for toggle-able components.
/// </summary>
/// <remarks>
/// If you are using <c>ItemToggleComponent</c> subscribe to <c>ItemToggledEvent</c> instead.
/// </remarks>
public sealed partial class InnerToggleableActionEvent : InstantActionEvent
{
    // SS220 checking the toggle value start
    [DataField]
    public bool InnerToggleableAction;
    // SS220 checking the toggle value end
}

/// <summary>
///     Generic enum keys for toggle-visualizer appearance data & sprite layers.
/// </summary>
[Serializable, NetSerializable]
public enum InnerToggleableVisuals : byte
{
    Toggled,
    Layer
}
