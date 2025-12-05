using System.Numerics;
using Content.Client.Lobby;
using Content.Shared.Paper;
using Content.Shared.Preferences;
using Content.Shared.SS220.Signature;

namespace Content.Client.SS220.Signature;

public sealed class SignatureSystem : SharedSignatureSystem
{
    [Dependency] private readonly IClientPreferencesManager _preferences = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaperComponent, ApplySavedSignature>(OnApplySavedSignature);
        SubscribeNetworkEvent<SendSignatureToAdminEvent>(OnSignature);
    }

    private void OnApplySavedSignature(Entity<PaperComponent> ent, ref ApplySavedSignature args)
    {
        var profile = _preferences.Preferences?.SelectedCharacter as HumanoidCharacterProfile;

        if (profile?.SignatureData == null)
            return;

        var state = new UpdateSignatureDataState(profile.SignatureData);
        _ui.SetUiState(ent.Owner, PaperComponent.PaperUiKey.Key, state);
    }

    private static void OnSignature(SendSignatureToAdminEvent ev)
    {
        var canvasSize = new Vector2(ev.Data.Width, ev.Data.Height);
        var window = new SignatureWindow(canvasSize);
        window.Signature.SetSignature(ev.Data);

        window.OpenCentered();
    }
}
