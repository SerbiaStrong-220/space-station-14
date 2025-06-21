using Content.Shared.SS220.GhostHearing;
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
    public Dictionary<IHearableChannelPrototype, bool> RadioChannels = new();

    [DataField]
    public Dictionary<IHearableChannelPrototype, bool> DisplayChannels = new();
}
//ss220 add filter tts for ghost end
