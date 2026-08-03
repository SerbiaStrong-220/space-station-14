using Content.Shared.SS220.TTS;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.VoiceMask;

[Serializable, NetSerializable]
public sealed class VoiceMaskChangeVoiceMessage(ProtoId<TtsVoicePrototype> voice) : BoundUserInterfaceMessage
{
    public readonly ProtoId<TtsVoicePrototype> Voice = voice;
}
