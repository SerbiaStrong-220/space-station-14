using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.TTS;

/// <summary>
/// Apply TTS for entity chat say messages
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class TtsComponent : Component
{
    [DataField, AutoNetworkedField]
    public TtsVoicePreferences VoicePreferences = TtsVoicePreferences.FromEnumerable(SharedTtsSystem.DefaultVoicePreferences);

    [DataField]
    public ProtoId<RandomVoicePreferencesPrototype>? RandomVoicePreferences;
}
