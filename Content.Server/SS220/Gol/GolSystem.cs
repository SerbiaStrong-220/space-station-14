// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Content.Shared.ActionBlocker;
using Robust.Shared.Audio.Systems;
using Content.Server.Chat.Systems;
using Content.Shared.SS220.Gol;
using Content.Shared.Dataset;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;


namespace Content.Server.SS220.Gol;

public sealed class GolSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GolComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GolComponent, GolActionEvent>(OnGolAction);
    }

    private void OnMapInit(Entity<GolComponent> uid, ref MapInitEvent args)
    {
        _actions.AddAction(uid, ref uid.Comp.GolActionEntity, uid.Comp.GolAction);
    }
    private void OnGolAction(Entity<GolComponent> uid, ref GolActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_actionBlocker.CanEmote(uid))
            return;

        var placeholder = _proto.Index<DatasetPrototype>(uid.Comp.GolPhrases);

        var emoteType = _random.Pick(placeholder.Values);

        _audio.PlayEntity(uid.Comp.GolSound, uid, uid);
        _chat.TrySendInGameICMessage(uid, emoteType, InGameICChatType.Emote, ChatTransmitRange.Normal, checkRadioPrefix: false, ignoreActionBlocker: true);

        args.Handled = true;
    }
}
