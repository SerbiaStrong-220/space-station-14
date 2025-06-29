using Content.Shared.Radio;
using Robust.Shared.GameStates;

namespace Content.Shared.Ghost;

//ss220 add filter tts for ghost start
[RegisterComponent]
[NetworkedComponent]
public sealed partial class GhostHearingComponent : Component
{
    [DataField]
    public bool IsEnabled;

    [DataField]
    public Dictionary<RadioChannelPrototype, bool> RadioChannels = new();

    [DataField]
    public Dictionary<RadioChannelPrototype, bool> DisplayChannels = new();
}
//ss220 add filter tts for ghost end
