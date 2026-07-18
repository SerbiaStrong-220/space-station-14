using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.TTS;

[Serializable, NetSerializable]
public sealed class RequestGlobalTTSEvent(string text, ProtoId<TTSVoicePrototype> voiceId) : EntityEventArgs
{
    public readonly string Text = text;
    public readonly ProtoId<TTSVoicePrototype> VoiceId = voiceId;
}
