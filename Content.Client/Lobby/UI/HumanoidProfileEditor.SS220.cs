using Content.Client.SS220.TTS.UI;
using Content.Shared.SS220.TTS;

namespace Content.Client.Lobby.UI;

public partial class HumanoidProfileEditor
{
    private void InitializeSS220()
    {
        TtsVoicePreferencesTable.OnPreferencesChanged += () => SetVoicePreferences(TtsVoicePreferencesTable.VoicePreferences);
    }

    private void UpdateSS220()
    {
        UpdateTtsVoicesControls();
        UpdateSignature();
    }

    private void UpdateTtsVoicesControls()
    {
        if (Profile is null)
            return;

        TtsVoicePreferencesTable.SetPreferences(Profile.VoicePreferences.Clone(), silent: true);
        TtsVoicePreferencesTable.RequirementsCheckData = new TtsVoiceRequirementCheckData
        {
            Session = _playerManager.LocalSession,
            Profile = Profile
        };
    }

    private void SetVoicePreferences(TtsVoicePreferences preferences)
    {
        Profile = Profile?.WithVoicePreferences(preferences.Clone());
        SetDirty();
    }
}
