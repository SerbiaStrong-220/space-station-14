// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Popups;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Speech;
using Content.Shared.SS220.Virology.Behaviors;

namespace Content.Server.SS220.Virology.Behaviors;

public sealed partial class VirusMuteSystem : EntitySystem
{
    [Dependency] private PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VirusMutedComponent, SpeakAttemptEvent>(OnSpeakAttempt);
        SubscribeLocalEvent<VirusMutedComponent, EmoteEvent>(OnEmote, before: [typeof(VocalSystem), typeof(MumbleAccentSystem)]);
        SubscribeLocalEvent<VirusMutedComponent, ScreamActionEvent>(OnScreamAction, before: [typeof(VocalSystem)]);
    }

    private void OnSpeakAttempt(Entity<VirusMutedComponent> ent, ref SpeakAttemptEvent args)
    {
        _popup.PopupEntity(Loc.GetString("speech-muted"), ent.Owner, ent.Owner);
        args.Cancel();
    }

    private void OnEmote(Entity<VirusMutedComponent> ent, ref EmoteEvent args)
    {
        if (args.Handled)
            return;

        if (args.Emote.Category.HasFlag(EmoteCategory.Vocal))
            args.Handled = true;
    }

    private void OnScreamAction(Entity<VirusMutedComponent> ent, ref ScreamActionEvent args)
    {
        if (args.Handled)
            return;

        _popup.PopupEntity(Loc.GetString("speech-muted"), ent.Owner, ent.Owner);
        args.Handled = true;
    }
}
