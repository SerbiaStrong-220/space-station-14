using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.TTS;

[Serializable, NetSerializable]
public sealed class RequestTTSVoiceTestEvent(ProtoId<TTSVoicePrototype> voiceId) : EntityEventArgs
{
    public readonly ProtoId<TTSVoicePrototype> VoiceId = voiceId;
}
