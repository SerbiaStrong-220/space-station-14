// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Content.Shared.ActionBlocker;
using Robust.Shared.Audio.Systems;
using Content.Server.Chat.Systems;
using Content.Shared.SS220.Goal;
using Content.Shared.Dataset;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;


namespace Content.Server.SS220.Goal;

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

        SubscribeLocalEvent<GoalComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GoalComponent, GoalActionEvent>(OnGolAction);
    }

    private void OnMapInit(Entity<GoalComponent> uid, ref MapInitEvent args)
    {
        _actions.AddAction(uid, ref uid.Comp.GoalActionEntity, uid.Comp.GoalAction);
    }
    private void OnGolAction(Entity<GoalComponent> uid, ref GoalActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!_actionBlocker.CanEmote(args.Performer))
            return;

        if (args.GoalSound != null)
        {
            _audio.PlayEntity(uid.Comp.GoalSound, uid, uid);
        }

        if (args.GoalPhrases != null)
        {
            var placeholder = _proto.Index<DatasetPrototype>(args.GoalPhrases);
            var emoteType = _random.Pick(placeholder.Values);
            _chat.TrySendInGameICMessage(uid, emoteType, InGameICChatType.Emote, ChatTransmitRange.Normal, checkRadioPrefix: false, ignoreActionBlocker: true);
        }
    }
}
