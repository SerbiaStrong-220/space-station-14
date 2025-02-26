// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Chat.Systems;
using Content.Server.Pinpointer;
using Content.Shared.Actions;
using Content.Shared.Clothing;
using Content.Shared.SS220.SmartGasMask;
using Content.Shared.SS220.SmartGasMask.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.SS220.SmartGasMask;

/// <summary>
/// This handles uses the radial menu to send canned messages.
/// </summary>
public sealed class SmartGasMaskSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    /// <inheritdoc/>
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

        if (args.ProtoId == ent.Comp.SelectablePrototypes[0] && !ent.Comp.OnCdHalt) //AlertSmartGasMaskHalt
        {
            ent.Comp.OnCdHalt = true;

            var haltMes = Loc.GetString(_random.Pick(ent.Comp.LocIdHaltMessage));

            _chatSystem.TrySendInGameICMessage(args.Actor, haltMes, InGameICChatType.Speak, ChatTransmitRange.Normal, checkRadioPrefix: false, ignoreActionBlocker: true);
            _audio.PlayPvs(ent.Comp.HaltSound, ent.Owner);

            Timer.Spawn(ent.Comp.CdTimeHalt, () => ent.Comp.OnCdHalt = false);
        }

        if (args.ProtoId == ent.Comp.SelectablePrototypes[1] && !ent.Comp.OnCdSupp) //AlertSmartGasMaskSupport
        {
            ent.Comp.OnCdSupp = true;

            var posText = FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString(ent.Owner));
            var helpMess = Loc.GetString(ent.Comp.LocIdSupportMessage, ("user", args.Actor), ("position", posText));

            //The message is sent with a prefix ".о". This is necessary so that everyone understands that reinforcements have been called in
            _chatSystem.TrySendInGameICMessage(args.Actor, helpMess, InGameICChatType.Whisper, ChatTransmitRange.Normal, checkRadioPrefix: true, ignoreActionBlocker: true);

            Timer.Spawn(ent.Comp.CdTimeSupp, () => ent.Comp.OnCdSupp = false);
        }
    }
}
