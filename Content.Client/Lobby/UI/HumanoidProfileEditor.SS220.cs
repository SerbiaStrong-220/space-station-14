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

        TtsVoicePreferencesTab.SetPreferences(Profile.VoicePreferences.Clone());
    }

    private void SetVoicePreferences(TtsVoicePreferences preferences)
    {
        Profile = Profile?.WithVoicePreferences(preferences.Clone());
        SetDirty();
    }
}
