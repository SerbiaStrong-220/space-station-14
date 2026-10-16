using Content.Client.SS220.TTS.UI;
using Content.Shared.SS220.TTS;
using Content.Shared.SS220.TTS.Requirements;
using Content.Shared.Traits;
using Robust.Shared.Prototypes;

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

    private bool TraitAllowedSpecies(TraitPrototype trait)
    {
        if (Profile == null)
            return true;

        if (trait.Whitelist == null && trait.Blacklist == null)
            return true;

        if (!_prototypeManager.TryIndex(Profile.Species, out var speciesProto))
            return true;

        if (!_prototypeManager.TryIndex<EntityPrototype>(speciesProto.Prototype, out var entityProto))
            return true;

        return _whitelistSystem.CheckBothPrototype(entityProto, trait.Blacklist, trait.Whitelist);
    }
}
