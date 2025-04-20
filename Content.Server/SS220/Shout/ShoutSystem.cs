// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.ActionBlocker;
using Robust.Shared.Audio.Systems;
using Content.Server.Chat.Systems;
using Content.Shared.SS220.Shout;
using Content.Shared.Dataset;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Shout;

/// <summary>
/// Just handler of a ShoutActionEvent.
/// If there is no sound or phrase it won't do anything.
/// </summary>

public sealed class ShoutSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShoutActionEvent>(OnShoutAction);
    }

    private void OnShoutAction(ShoutActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!_actionBlocker.CanEmote(args.Performer))
            return;

        if (args.ShoutSound != null)
        {
            _audio.PlayPvs(args.ShoutSound,  args.Performer);
        }

        if (args.ShoutPhrases != null)
        {
            if (!_proto.TryIndex<LocalizedDatasetPrototype>(args.ShoutPhrases, out var placeholder))//i dont like nested ifs, but idk how to make it more pretty
                return;

            var localizedPhrase = Loc.GetString(_random.Pick(placeholder.Values));
            _chat.TrySendInGameICMessage(args.Performer, localizedPhrase, InGameICChatType.Emote, ChatTransmitRange.Normal, checkRadioPrefix: false, ignoreActionBlocker: true);
        }
    }
}
