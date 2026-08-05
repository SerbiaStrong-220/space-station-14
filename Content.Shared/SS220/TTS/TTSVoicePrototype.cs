using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.TTS;

/// <summary>
/// Prototype represent available TTS voices
/// </summary>
[Prototype]
public sealed partial class TtsVoicePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name = string.Empty;

    [DataField]
    public string Description = string.Empty;

    [DataField]
    public List<ProtoId<TtsVoiceCategoryPrototype>> Categories = [];

    [DataField]
    public TtsProvider Provider = TtsProvider.NTTS;

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
