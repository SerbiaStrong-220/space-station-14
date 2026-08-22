using Content.Shared.SS220.TTS.Requirements;
using Content.Shared.SS220.TTS.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.TTS.Prototypes;

/// <summary>
/// Prototype represent available TTS voices
/// </summary>
[Prototype]
public sealed partial class TtsVoicePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name = string.Empty;
    public string LocalizedName => Loc.GetString(Name);

    [DataField]
    public LocId Description = string.Empty;
    public string LocalizedDescription => string.IsNullOrEmpty(Description) ? string.Empty : Loc.GetString(Description);

    [DataField]
    public List<ProtoId<TtsVoiceCategoryPrototype>> Categories = [];

    [DataField]
    public TtsProvider Provider = TtsProvider.NTTS;

    [DataField(required: true)]
    public string Speaker = string.Empty;

    [DataField]
    public TtsVoiceRequirement? Requirement;

    /// <summary>
    /// Whether the voice is hidden from the voice editors
    /// </summary>
    [DataField]
    public bool EditorHidden = false;
}
