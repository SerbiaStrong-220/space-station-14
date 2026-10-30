using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.TTS;

/// <summary>
/// Apply TTS for entity chat say messages
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState(true)]
// ReSharper disable once InconsistentNaming
public sealed partial class TTSComponent : Component
{
    /// <summary>
    /// Prototype of used voice for TTS.
    /// </summary>
    [DataField("voice"), AutoNetworkedField]
    public ProtoId<TTSVoicePrototype>? VoicePrototypeId { get; set; }

    /// <summary>
    /// Prototype that contains a list of voices for randomize
    /// </summary>
    [DataField("randomVoicesList")]
    public ProtoId<RandomVoicesListPrototype>? RandomVoicesList { get; private set; }
}
