using Content.Shared.SS220.VoiceMask;
using Content.Shared.VoiceMask;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.VoiceMask;

public sealed class VoiceMaskBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _protomanager = default!;

    [ViewVariables]
    private VoiceMaskNameChangeWindow? _window;

    public VoiceMaskBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<VoiceMaskNameChangeWindow>();
        _window.ReloadVerbs(_protomanager);
        _window.AddVerbs();

        _window.OnNameChange += OnNameSelected;
        _window.OnVoicePreferencesChanged += pref => SendMessage(new VoiceMaskChangeTtsVoicePreferencesMessage(pref)); // SS220 tts
        _window.OnVerbChange += verb => SendMessage(new VoiceMaskChangeVerbMessage(verb));
        _window.OnToggle += OnToggle;
        _window.OnAccentToggle += OnAccentToggle;
    }

    private void OnNameSelected(string name)
    {
        SendMessage(new VoiceMaskChangeNameMessage(name));
    }

    private void OnToggle()
    {
        SendMessage(new VoiceMaskToggleMessage());
    }

    private void OnAccentToggle()
    {
        SendMessage(new VoiceMaskAccentToggleMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not VoiceMaskBuiState cast || _window == null)
        {
            return;
        }

        _window.UpdateState(cast.Name, cast.Verb, cast.Active, cast.AccentHide, cast.VoicePreferences /* SS220 tts */);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _window?.Close();
    }
}
