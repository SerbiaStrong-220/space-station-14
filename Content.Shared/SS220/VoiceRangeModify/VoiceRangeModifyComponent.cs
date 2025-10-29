using Robust.Shared.GameStates;

namespace Content.Shared.SS220.VoiceRangeModify;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class VoiceRangeModifyComponent : Component
{
    [DataField]
    public float VoiceRange = 10f;

    [DataField("whisperClearRange")]
    public float BaseWhisperClearRange = 2f;

    [DataField("whisperMuffledRange")]
    public float BaseWhisperMuffledRange = 5f;
}
