using Content.Client.SS220.TTS.UI;
using Content.Shared.SS220.TTS;

namespace Content.Client.Lobby.UI;

public partial class HumanoidProfileEditor
{
    private void InitializeSS220()
    {
        TtsVoicePreferencesTab.OnPreferencesChanged += () => SetVoicePreferences(TtsVoicePreferencesTab.VoicePreferences);
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

        TtsVoicePreferencesTab.SetPreferences(Profile.VoicePreferences.Clone(), silent: true);
        TtsVoicePreferencesTab.RequirementsCheckData = new TtsVoiceRequirementCheckData
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
