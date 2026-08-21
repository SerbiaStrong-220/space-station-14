// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.SS220.DialogWindowDescUI;
using Content.Client.SS220.DialogWindowTtsVoicePreferencesUI;
using Content.Shared.Administration;
using Content.Shared.Humanoid;
using Content.Shared.SS220.TTS;
using Robust.Client.Player;

namespace Content.Client.SS220.QuickDialog;

public sealed partial class QuickDialogSystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeNetworkEvent<QuickDialogDescOpenEvent>(OpenDialog);
        SubscribeNetworkEvent<QuickDialogTtsVoicePreferencesOpenEvent>(OpenDialogVoicePreferences);
    }

    private void OpenDialog(QuickDialogDescOpenEvent ev)
    {
        var ok = (ev.Buttons & QuickDialogButtonFlag.OkButton) != 0;
        var window = new DialogWindowDesc(ev.Title, ev.Description, ev.Prompts, ok: ok);

        window.OnConfirmed += responses =>
        {
            RaiseNetworkEvent(new QuickDialogResponseEvent(ev.DialogId,
                responses,
                QuickDialogButtonFlag.OkButton));
        };

        window.OnCancelled += () =>
        {
            RaiseNetworkEvent(new QuickDialogResponseEvent(ev.DialogId,
                [],
                QuickDialogButtonFlag.CancelButton));
        };
    }

    private void OpenDialogVoicePreferences(QuickDialogTtsVoicePreferencesOpenEvent ev)
    {
        var targetUid = GetEntity(ev.Target);

        var ok = (ev.Buttons & QuickDialogButtonFlag.OkButton) != 0;

        if (targetUid is not { Valid: true } uid)
        {
            CancelDialog(ev);
            return;
        }

        MakeDialogTtsVoicePreferences(uid, ev, ok);
    }

    private void CancelDialog(QuickDialogTtsVoicePreferencesOpenEvent ev)
    {
        RaiseNetworkEvent(new QuickDialogResponseEvent(ev.DialogId,
            [],
            QuickDialogButtonFlag.CancelButton));
    }

    private void MakeDialogTtsVoicePreferences(EntityUid target, QuickDialogTtsVoicePreferencesOpenEvent ev, bool ok)
    {
        if (ev.Prompts.Count != 1)
        {
            CancelDialog(ev);
            return;
        }

        if (!TryComp<HumanoidProfileComponent>(target, out var humanoidProfile))
        {
            CancelDialog(ev);
            return;
        }

        TryComp<TtsComponent>(target, out var ttsComp);

        var voiceCheckData = new TtsVoiceRequirementCheckData()
        {
            Session = _player.LocalSession,
            Entity = target
        };

        var window = new DialogWindowTtsVoicePreferences(ev.Title,
            ev.Description,
            ev.Prompts[0],
            voiceCheckData: voiceCheckData,
            preferences: ttsComp?.VoicePreferencesRO.Clone(),
            ok: ok);

        window.OnConfirmed += (promptFieldId, newPreferences) =>
        {
            RaiseNetworkEvent(new QuickDialogResponseEvent(ev.DialogId,
                new() { [promptFieldId] = newPreferences.ToString() },
                QuickDialogButtonFlag.OkButton));
        };

        window.OnCancelled += () => CancelDialog(ev);
    }
}

