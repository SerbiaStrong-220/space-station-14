using Content.Shared.Radio;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Headset;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class HeadsetToggledComponent : Component
{
    public readonly Dictionary<RadioChannelPrototype, bool> RadioChannels = new();
}
