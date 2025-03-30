// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Chat.Systems;
using Content.Server.Pinpointer;
using Content.Shared.Actions;
using Content.Shared.Clothing;
using Content.Shared.SS220.SmartGasMask;
using Content.Shared.SS220.SmartGasMask.Events;
using Content.Shared.SS220.SmartGasMask.Prototype;
using Robust.Server.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.SS220.SmartGasMask;

/// <summary>
/// This handles uses the radial menu to send canned messages.
/// </summary>
public sealed class SmartGasMaskSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SmartGasMaskComponent, SmartGasMaskOpenEvent>(OnAction);
        SubscribeLocalEvent<SmartGasMaskComponent, SmartGasMaskMessage>(OnChoose);
        SubscribeLocalEvent<SmartGasMaskComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<SmartGasMaskComponent, ClothingGotUnequippedEvent>(OnUnequipped);
    }

    private void OnEquipped(Entity<SmartGasMaskComponent> ent, ref ClothingGotEquippedEvent args)
    {
        _actions.AddAction(args.Wearer, ref ent.Comp.SmartGasMaskActionEntity, ent.Comp.SmartGasMaskAction, ent.Owner);
    }

    private void OnUnequipped(Entity<SmartGasMaskComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        _actions.RemoveProvidedActions(args.Wearer, ent.Owner);
    }

    private void OnAction(Entity<SmartGasMaskComponent> ent, ref SmartGasMaskOpenEvent args)
    {
        if (args.Handled || !TryComp<ActorComponent>(args.Performer, out var actor))
            return;

        args.Handled = true;

        _userInterface.TryToggleUi((ent.Owner, null), SmartGasMaskUiKey.Key, actor.PlayerSession);
    }

    private void OnChoose(Entity<SmartGasMaskComponent> ent, ref SmartGasMaskMessage args)
    {
        var curTime = _timing.CurTime;

        if(!ent.Comp.SelectablePrototypes.Contains(args.ProtoId))
            return;

        if (!_prototypeManager.TryIndex(args.ProtoId, out var alertProto))
                return;

        if (ent.Comp.NextChargeTime.TryGetValue(args.ProtoId, out var nextChargeTime) && curTime < nextChargeTime)
            return;

        ent.Comp.NextChargeTime[args.ProtoId] = curTime + alertProto.CoolDown;

        if (alertProto.NotificationType == NotificationType.Halt) //AlertSmartGasMaskHalt
        {
            _audio.PlayPvs(alertProto.AlertSound, ent.Owner);

            if(alertProto.LocIdMessage.Count == 0)
                return;

            var haltMessage = Loc.GetString(_random.Pick(alertProto.LocIdMessage));

            _chatSystem.TrySendInGameICMessage(args.Actor, haltMessage, InGameICChatType.Speak, ChatTransmitRange.Normal, checkRadioPrefix: false, ignoreActionBlocker: true);
        }

        if (alertProto.NotificationType == NotificationType.Support) //AlertSmartGasMaskSupport
        {
            if(alertProto.LocIdMessage.Count == 0)
                return;

            var currentHelpMessage = _random.Pick(alertProto.LocIdMessage);
            var posText = FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString(ent.Owner));
            var helpMessage = Loc.GetString(currentHelpMessage, ("user", args.Actor), ("position", posText));

            //The message is sent with a prefix ".о". This is necessary so that everyone understands that reinforcements have been called in
            _chatSystem.TrySendInGameICMessage(args.Actor, helpMessage, InGameICChatType.Whisper, ChatTransmitRange.Normal, checkRadioPrefix: true, ignoreActionBlocker: true);
        }
    }
}
