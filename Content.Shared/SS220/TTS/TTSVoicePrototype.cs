using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.TTS;

/// <summary>
/// Prototype represent available TTS voices
/// </summary>
[Prototype("ttsVoice")]
public sealed partial class TTSVoicePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name = string.Empty;

    [DataField]
    public string Description = string.Empty;

    [DataField]
    public TTSProvider Provider = TTSProvider.NTTS;

    [DataField(required: true)]
    public string Speaker = string.Empty;

    [DataField(required: true)]
    public Sex Sex;

    /// <summary>
    /// Whether the species is available "at round start" (In the character editor)
    /// </summary>
    [DataField]
    public bool RoundStart = true;

    [DataField]
    public bool SponsorOnly = false;
}

public sealed class TTSVoiceDef : Dictionary<TTSProvider, ProtoId<TTSVoicePrototype>> { }
