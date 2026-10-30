// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.SS220.DialogWindowDescUI;
using Content.Client.SS220.DialogWindowProtoIdUI;
using Content.Client.SS220.TTS;
using Content.Shared.Administration;
using Content.Shared.Humanoid;
using Content.Shared.SS220.TTS;

namespace Content.Client.SS220.QuickDialog;

public sealed class QuickDialogSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeNetworkEvent<QuickDialogDescOpenEvent>(OpenDialog);
        SubscribeNetworkEvent<QuickDialogTTSProtoIdOpenEvent>(OpenDialogProtoId);
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
                new(),
                QuickDialogButtonFlag.CancelButton));
        };
    }

    private void OpenDialogProtoId(QuickDialogTTSProtoIdOpenEvent ev)
    {
        var targetUid = GetEntity(ev.Target);

        var ok = (ev.Buttons & QuickDialogButtonFlag.OkButton) != 0;

        if (targetUid is not { Valid: true } uid)
        {
            CancelDialog(ev);
            return;
        }

        MakeDialogTTSProtoId(uid, ev, ok);
    }

    private void CancelDialog(QuickDialogTTSProtoIdOpenEvent ev)
    {
        RaiseNetworkEvent(new QuickDialogResponseEvent(ev.DialogId,
            new(),
            QuickDialogButtonFlag.CancelButton));
    }

    private void MakeDialogTTSProtoId(EntityUid target, QuickDialogTTSProtoIdOpenEvent ev, bool ok)
    {
        if (!TryComp<HumanoidProfileComponent>(target, out var humanoidProfile))
        {
            CancelDialog(ev);
            return;
        }

        var currentId = CompOrNull<TTSComponent>(target)?.VoicePrototypeId;

        var window = new DialogWindowTTSProtoId(ev.Title, ev.Description, ev.Prompts,
        prototype =>
        {
            if (prototype is not TTSVoicePrototype voicePrototype)
                return false;

            return voicePrototype.RoundStart && (voicePrototype.Sex == Sex.Unsexed || humanoidProfile.Sex == Sex.Unsexed || voicePrototype.Sex == humanoidProfile.Sex);
        }, ok: ok, currentVoiceId: currentId);

        window.OnConfirmed += responses =>
        {
            RaiseNetworkEvent(new QuickDialogResponseEvent(ev.DialogId,
                responses,
                QuickDialogButtonFlag.OkButton));
        };

        window.OnCancelled += () => CancelDialog(ev);
    }
}

