using Content.Shared.Speech;
using Content.Shared.SS220.Language.Systems;
using Content.Shared.SS220.TTS; // SS220 Tape recorder TTS
using Robust.Shared.Prototypes;

namespace Content.Shared.TapeRecorder;

/// <summary>
/// Every chat event recorded on a tape is saved in this format
/// </summary>
[ImplicitDataDefinitionForInheritors]
public sealed partial class TapeCassetteRecordedMessage : IComparable<TapeCassetteRecordedMessage>
{
    /// <summary>
    /// Number of seconds since the start of the tape that this event was recorded at
    /// </summary>
    [DataField(required: true)]
    public float Timestamp = 0;

    /// <summary>
    /// The name of the entity that spoke
    /// </summary>
    [DataField]
    public string? Name;

    /// <summary>
    /// The verb used for this message.
    /// </summary>
    [DataField]
    public ProtoId<SpeechVerbPrototype>? Verb;

    /// <summary>
    /// What was spoken
    /// </summary>
    [DataField]
    public string Message = string.Empty;

    // SS220 Tape recorder TTS begin
    /// <summary>
    /// TTS Voice used for this message.
    /// </summary>
    [DataField]
    public ProtoId<TTSVoicePrototype>? TtsVoice;
    // SS220 Tape recorder TTS end

    // SS220 languages begin
    [DataField]
    public LanguageMessage? LanguageMessage;
    // SS220 languages end

    // SS220 Tape recorder TTS begin
    //public TapeCassetteRecordedMessage(float timestamp, string name, ProtoId<SpeechVerbPrototype> verb, string message)
    public TapeCassetteRecordedMessage(float timestamp, string name, ProtoId<SpeechVerbPrototype> verb, string message, ProtoId<TTSVoicePrototype>? ttsVoice, LanguageMessage? languageMessage /* SS220 languages*/)
    // SS220 Tape recorder TTS end
    {
        Timestamp = timestamp;
        Name = name;
        Verb = verb;
        Message = message;
        TtsVoice = ttsVoice; // SS220 Tape recorder TTS
        LanguageMessage = languageMessage; // SS220 languages
    }

    public int CompareTo(TapeCassetteRecordedMessage? other)
    {
        if (other == null)
            return 0;

        return (int) (Timestamp - other.Timestamp);
    }
}
