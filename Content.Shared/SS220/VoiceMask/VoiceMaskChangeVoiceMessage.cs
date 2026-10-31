using Content.Shared.SS220.TTS;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.VoiceMask;

[Serializable, NetSerializable]
public sealed class VoiceMaskChangeTtsVoicePreferencesMessage(TtsVoicePreferences preferences) : BoundUserInterfaceMessage
{
    public readonly TtsVoicePreferences VoicePreferences = preferences;
}
