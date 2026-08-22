using Content.Shared.SS220.TTS.Prototypes;
using Content.Shared.SS220.TTS.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.TTS.Components;

/// <summary>
/// Apply TTS for entity chat say messages
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedTtsSystem), Other = AccessPermissions.Read)]
public sealed partial class TtsComponent : Component
{
    [Access(Other = AccessPermissions.ReadExecute)]
    public IReadOnlyTtsVoicePreferences VoicePreferencesRO => VoicePreferences;

    [DataField(customTypeSerializer: typeof(TtsVoicePreferencesSerializer)), AutoNetworkedField]
    public TtsVoicePreferences VoicePreferences = TtsVoicePreferences.FromEnumerable(SharedTtsSystem.DefaultVoicePreferences);

    [DataField]
    public ProtoId<RandomVoicePreferencesPrototype>? RandomVoicePreferences;
}
