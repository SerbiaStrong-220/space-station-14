// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Server.Power.Components;
using Content.Server.Radio.Components;
using Content.Server.VoiceMask;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Radio;
using Content.Shared.SS220.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.SS220.Radio.Components;
using Content.Shared.Speech;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Replays;
using Robust.Shared.Utility;
using Content.Shared.Access.Components;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using System.Globalization;
using Content.Server.Popups;

namespace Content.Server.Radio.EntitySystems;

/// <summary>
///     This system handles intrinsic radios and the general process of converting radio messages into chat messages.
/// </summary>
public sealed class RadioSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly IReplayRecordingManager _replay = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    // set used to prevent radio feedback loops.
    private readonly HashSet<string> _messages = new();

    private EntityQuery<TelecomExemptComponent> _exemptQuery;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IntrinsicRadioReceiverComponent, RadioReceiveEvent>(OnIntrinsicReceive);
        SubscribeLocalEvent<IntrinsicRadioTransmitterComponent, EntitySpokeEvent>(OnIntrinsicSpeak);

        //SS220 PAI with encryption keys begin
        SubscribeLocalEvent<IntrinsicRadioReceiverComponent, EncryptionChannelsChangedEvent>(OnEncryptionChannelsChangeReceiver);
        SubscribeLocalEvent<IntrinsicRadioTransmitterComponent, EncryptionChannelsChangedEvent>(OnEncryptionChannelsChangeTransmitter);
        //SS220 PAI with encryption keys end

        _exemptQuery = GetEntityQuery<TelecomExemptComponent>();
    }

    private void OnIntrinsicSpeak(EntityUid uid, IntrinsicRadioTransmitterComponent component, EntitySpokeEvent args)
    {
        if (args.Channel != null && (component.Channels.Contains(args.Channel.ID) ||
            component.EncryptionKeyChannels.Contains(args.Channel.ID))) //SS220 PAI with encryption keys
        {
            SendRadioMessage(uid, args.Message, args.Channel, uid);
            args.Channel = null; // prevent duplicate messages from other listeners.
        }
    }

    private void OnIntrinsicReceive(EntityUid uid, IntrinsicRadioReceiverComponent component, ref RadioReceiveEvent args)
    {
        if (TryComp(uid, out ActorComponent? actor))
            _netMan.ServerSendMessage(args.ChatMsg, actor.PlayerSession.Channel);
    }

    /// <summary>
    /// Send radio message to all active radio listeners
    /// </summary>
    public void SendRadioMessage(EntityUid messageSource, string message, ProtoId<RadioChannelPrototype> channel, EntityUid radioSource, bool escapeMarkup = true)
    {
        SendRadioMessage(messageSource, message, _prototype.Index(channel), radioSource, escapeMarkup: escapeMarkup);
    }

    /// <summary>
    /// Send radio message to all active radio listeners
    /// </summary>
    /// <param name="messageSource">Entity that spoke the message</param>
    /// <param name="radioSource">Entity that picked up the message and will send it, e.g. headset</param>
    public void SendRadioMessage(EntityUid messageSource, string message, RadioChannelPrototype channel, EntityUid radioSource, bool escapeMarkup = true)
    {
        // TODO if radios ever garble / modify messages, feedback-prevention needs to be handled better than this.
        if (!_messages.Add(message))
            return;

        var name = TryComp(messageSource, out VoiceMaskComponent? mask) && mask.Enabled
            ? mask.VoiceName
            : MetaData(messageSource).EntityName;

        name = FormattedMessage.EscapeText(name);

        // SS220 department-radio-color
        var formattedName = $"[color={GetIdCardColor(messageSource)}]{GetIdCardName(messageSource)}{name}[/color]";

        SpeechVerbPrototype speech;
        if (mask != null
            && mask.Enabled
            && mask.SpeechVerb != null
            && _prototype.TryIndex<SpeechVerbPrototype>(mask.SpeechVerb, out var proto))
        {
            speech = proto;
        }
        else
            speech = _chat.GetSpeechVerb(messageSource, message);

        var content = escapeMarkup
            ? FormattedMessage.EscapeText(message)
            : message;

        if (GetIdCardIsBold(messageSource))
        {
            content = $"[bold]{content}[/bold]";
        }

        var wrappedMessage = Loc.GetString(speech.Bold ? "chat-radio-message-wrap-bold" : "chat-radio-message-wrap",
            ("color", channel.Color),
            ("fontType", speech.FontId),
            ("fontSize", speech.FontSize),
            ("verb", Loc.GetString(_random.Pick(speech.SpeechVerbStrings))),
            ("channel", $"\\[{channel.LocalizedName}\\]"),
            ("name", formattedName),
            ("message", content));

        // most radios are relayed to chat, so lets parse the chat message beforehand
        var chat = new ChatMessage(
            ChatChannel.Radio,
            message,
            wrappedMessage,
            NetEntity.Invalid,
            null);
        var chatMsg = new MsgChatMessage { Message = chat };
        var ev = new RadioReceiveEvent(message, messageSource, channel, radioSource, chatMsg, new());

        var sendAttemptEv = new RadioSendAttemptEvent(channel, radioSource);
        RaiseLocalEvent(ref sendAttemptEv);
        RaiseLocalEvent(radioSource, ref sendAttemptEv);
        var canSend = !sendAttemptEv.Cancelled;

        var sourceMapId = Transform(radioSource).MapID;
        var hasActiveServer = HasActiveServer(sourceMapId, channel.ID);
        var sourceServerExempt = _exemptQuery.HasComp(radioSource);

        var radioQuery = EntityQueryEnumerator<ActiveRadioComponent, TransformComponent>();
        while (canSend && radioQuery.MoveNext(out var receiver, out var radio, out var transform))
        {
            if (!radio.ReceiveAllChannels)
            {
                if (!radio.Channels.Contains(channel.ID) || (TryComp<IntercomComponent>(receiver, out var intercom) &&
                                                             !intercom.SupportedChannels.Contains(channel.ID)))
                    continue;
            }

            if (!channel.LongRange && transform.MapID != sourceMapId && !radio.GlobalReceive)
                continue;

            // don't need telecom server for long range channels or handheld radios and intercoms
            var needServer = !channel.LongRange && !sourceServerExempt;
            if (needServer && !hasActiveServer)
                continue;

            // check if message can be sent to specific receiver
            var attemptEv = new RadioReceiveAttemptEvent(channel, radioSource, receiver);
            RaiseLocalEvent(ref attemptEv);
            RaiseLocalEvent(receiver, ref attemptEv);
            if (attemptEv.Cancelled)
                continue;

            // send the message
            RaiseLocalEvent(receiver, ref ev);
        }

        // Dispatch TTS radio speech event for every receiver
        RaiseLocalEvent(new RadioSpokeEvent(messageSource, message, ev.Receivers.ToArray()));

        if (name != Name(messageSource))
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Radio message from {ToPrettyString(messageSource):user} as {name} on {channel.LocalizedName}: {message}");
        else
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Radio message from {ToPrettyString(messageSource):user} on {channel.LocalizedName}: {message}");

        _replay.RecordServerMessage(chat);
        _messages.Remove(message);
    }

    private IdCardComponent? GetIdCard(EntityUid senderUid)
    {
        if (!_inventorySystem.TryGetSlotEntity(senderUid, "id", out var idUid))
            return null;

        if (EntityManager.TryGetComponent(idUid, out PdaComponent? pda) && pda.ContainedId is not null)
        {
            // PDA
            if (TryComp<IdCardComponent>(pda.ContainedId, out var idComp))
                return idComp;
        }
        else if (EntityManager.TryGetComponent(idUid, out IdCardComponent? id))
        {
            // ID Card
            return id;
        }

        return null;
    }

    // SS220 radio-department-tag begin
    private string GetIdCardName(EntityUid senderUid)
    {
        var idCardTitle = Loc.GetString("chat-radio-no-id");
        idCardTitle = GetIdCard(senderUid)?.JobTitle ?? idCardTitle;

        var textInfo = CultureInfo.CurrentCulture.TextInfo;
        idCardTitle = textInfo.ToTitleCase(idCardTitle);

        return $"\\[{idCardTitle}\\] ";
    }
    // S220 radio-department-tag end

    // SS220 department-radio-color begin
    private string GetIdCardColor(EntityUid senderUid)
    {
        var color = GetIdCard(senderUid)?.JobColor;
        return (!string.IsNullOrEmpty(color)) ? color : "#9FED58";
    }

    private bool GetIdCardIsBold(EntityUid senderUid)
    {
        return GetIdCard(senderUid)?.RadioBold ?? false;
    }
    // SS220 department-radio-color end

    /// <inheritdoc cref="TelecomServerComponent"/>
    private bool HasActiveServer(MapId mapId, string channelId)
    {
        var servers = EntityQuery<TelecomServerComponent, EncryptionKeyHolderComponent, ApcPowerReceiverComponent, TransformComponent>();
        foreach (var (_, keys, power, transform) in servers)
        {
            if (transform.MapID == mapId &&
                power.Powered &&
                keys.Channels.Contains(channelId))
            {
                return true;
            }
        }
        return false;
    }

    //SS220 PAI with encryption keys begin
    private void OnEncryptionChannelsChangeTransmitter(Entity<IntrinsicRadioTransmitterComponent> entity, ref EncryptionChannelsChangedEvent args)
    {
        if (args.Component.Channels.Count == 0)
            entity.Comp.EncryptionKeyChannels.Clear();
        else
            entity.Comp.EncryptionKeyChannels = new(args.Component.Channels);
    }

    private void OnEncryptionChannelsChangeReceiver(Entity<IntrinsicRadioReceiverComponent> entity, ref EncryptionChannelsChangedEvent args)
    {
        if (!TryComp<ActiveRadioComponent>(entity.Owner, out var activeRadio))
            return;

        HashSet<string> channels = entity.Comp.Channels;
        channels.UnionWith(args.Component.Channels);

        activeRadio.Channels = new(channels);
    }
    //SS220 PAI with encryption keys end
}
