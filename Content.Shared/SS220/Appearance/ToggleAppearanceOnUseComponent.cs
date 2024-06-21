// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Appearance;

[RegisterComponent, NetworkedComponent]
public sealed partial class ToggleAppearanceOnUseComponent : Component
{
    /// <summary>
    ///     The current state of component appearance
    /// </summary>
    [ViewVariables, DataField("enabled")] public bool IsEnabled;
}

[Serializable, NetSerializable]
public sealed class ToggleAppearanceOnUseState : ComponentState
{
    public bool IsEnabled;

    public ToggleAppearanceOnUseState(bool isEnabled)
    {
        IsEnabled = isEnabled;
    }
}
