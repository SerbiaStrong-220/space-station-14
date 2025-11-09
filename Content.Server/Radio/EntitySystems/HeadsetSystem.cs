using Content.Server.Chat.Systems;
using Content.Shared.SS220.TTS;
using Content.Shared.Inventory.Events;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Radio.EntitySystems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Content.Server.SS220.Language;
using Content.Shared.Inventory;
using System.Linq;
using Content.Shared.SS220.Radio.Components;
using Content.Shared.SS220.Headset; // SS220-Add-Languages

namespace Content.Server.Radio.EntitySystems;

public sealed class HeadsetSystem : SharedHeadsetSystem
{
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly RadioSystem _radio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HeadsetComponent, RadioReceiveEvent>(OnHeadsetReceive);
        SubscribeLocalEvent<HeadsetComponent, EncryptionChannelsChangedEvent>(OnKeysChanged);
        SubscribeLocalEvent<HeadsetComponent, SendLanguageMessageAttemptEvent>(OnSendLangaugeMessageAttempt); // SS220 languages

        SubscribeLocalEvent<WearingHeadsetComponent, EntitySpokeEvent>(OnSpeak);
        // SS220-add-frequency-radio-begin
        SubscribeLocalEvent<EncryptionKeyHolderComponent, RadioSendAttemptEvent>(OnRadioSendAttemptEvent);
        SubscribeLocalEvent<EncryptionKeyHolderComponent, RadioReceiveAttemptEvent>(OnRadioReceiveEvent);
        SubscribeLocalEvent<EncryptionKeyHolderComponent, HeadsetChangeFrequencyMessage>(OnHeadsetChangeFrequency);
        // SS220-add-frequency-radio-end
    }
    // SS220-add-frequency-radio-begin
    private void OnRadioSendAttemptEvent(Entity<EncryptionKeyHolderComponent> entity, ref RadioSendAttemptEvent args)
    {
        if (args.Frequency is null)
            return;

        if (!entity.Comp.Channels.Contains(args.Channel))
            return;

        var targetChannel = args.Channel.ID;
        foreach (var keyEntity in entity.Comp.KeyContainer.ContainedEntities)
        {
            if (!TryComp<EncryptionKeyComponent>(keyEntity, out var encryptionKeyComponent)
                || !encryptionKeyComponent.Channels.Contains(targetChannel))
                continue;

            if (!TryComp<RadioEncryptionKeyComponent>(keyEntity, out var radioEncryptionKey))
                continue;

            args.Cancelled = radioEncryptionKey.RadioFrequency != args.Frequency;
            return;
        }
    }

    private void OnRadioReceiveEvent(Entity<EncryptionKeyHolderComponent> entity, ref RadioReceiveAttemptEvent args)
    {
        if (args.Frequency is null)
            return;

        if (!entity.Comp.Channels.Contains(args.Channel))
            return;

        var targetChannel = args.Channel.ID;
        foreach (var keyEntity in entity.Comp.KeyContainer.ContainedEntities)
        {
            if (!TryComp<EncryptionKeyComponent>(keyEntity, out var encryptionKeyComponent)
                || !encryptionKeyComponent.Channels.Contains(targetChannel))
                continue;

            if (!TryComp<RadioEncryptionKeyComponent>(keyEntity, out var radioEncryptionKey))
                continue;

            args.Cancelled = radioEncryptionKey.RadioFrequency != args.Frequency;
            return;
        }
    }

    private void OnHeadsetChangeFrequency(Entity<EncryptionKeyHolderComponent> entity, ref HeadsetChangeFrequencyMessage args)
    {
        foreach (var keyEntity in entity.Comp.KeyContainer.ContainedEntities)
        {
            if (!TryComp<RadioEncryptionKeyComponent>(keyEntity, out var radioEncryptionKey))
                continue;

            radioEncryptionKey.RadioFrequency = args.Frequency;
            Dirty(keyEntity, radioEncryptionKey);
            return;
        }
    }
    // SS220-add-frequency-radio-end

    private void OnKeysChanged(EntityUid uid, HeadsetComponent component, EncryptionChannelsChangedEvent args)
    {
        UpdateRadioChannels(uid, component, args.Component);
    }

    private void UpdateRadioChannels(EntityUid uid, HeadsetComponent headset, EncryptionKeyHolderComponent? keyHolder = null)
    {
        // make sure to not add ActiveRadioComponent when headset is being deleted
        if (!headset.Enabled || MetaData(uid).EntityLifeStage >= EntityLifeStage.Terminating)
            return;

        if (!Resolve(uid, ref keyHolder))
            return;

        if (keyHolder.Channels.Count == 0)
            RemComp<ActiveRadioComponent>(uid);
        else
            EnsureComp<ActiveRadioComponent>(uid).Channels = new(keyHolder.Channels);
    }

    private void OnSpeak(EntityUid uid, WearingHeadsetComponent component, EntitySpokeEvent args)
    {
        if (args.Channel != null
            && TryComp(component.Headset, out EncryptionKeyHolderComponent? keys)
            && keys.Channels.Contains(args.Channel.ID))
        {
            _radio.SendRadioMessage(uid, args.Message, args.Channel, component.Headset, languageMessage: args.LanguageMessage /* SS220 languages */, frequency: args.Frequency /* SS220 radio channels */);
            args.Channel = null; // prevent duplicate messages from other listeners.
            args.Frequency = null; // SS220 radio channels (not tested I believe to up one message)
        }
    }

    protected override void OnGotEquipped(EntityUid uid, HeadsetComponent component, GotEquippedEvent args)
    {
        base.OnGotEquipped(uid, component, args);
        if (component.IsEquipped && component.Enabled)
        {
            EnsureComp<WearingHeadsetComponent>(args.Equipee).Headset = uid;
            UpdateRadioChannels(uid, component);
        }
    }

    protected override void OnGotUnequipped(EntityUid uid, HeadsetComponent component, GotUnequippedEvent args)
    {
        base.OnGotUnequipped(uid, component, args);
        RemComp<ActiveRadioComponent>(uid);
        RemComp<WearingHeadsetComponent>(args.Equipee);
    }

    public void SetEnabled(EntityUid uid, bool value, HeadsetComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Enabled == value)
            return;

        component.Enabled = value;
        Dirty(uid, component);

        if (!value)
        {
            RemCompDeferred<ActiveRadioComponent>(uid);

            if (component.IsEquipped)
                RemCompDeferred<WearingHeadsetComponent>(Transform(uid).ParentUid);
        }
        else if (component.IsEquipped)
        {
            EnsureComp<WearingHeadsetComponent>(Transform(uid).ParentUid).Headset = uid;
            UpdateRadioChannels(uid, component);
        }
    }

    private void OnHeadsetReceive(EntityUid uid, HeadsetComponent component, ref RadioReceiveEvent args)
    {
        var parent = Transform(uid).ParentUid;

        if (parent.IsValid())
        {
            var relayEvent = new HeadsetRadioReceiveRelayEvent(args);
            RaiseLocalEvent(parent, ref relayEvent);
        }

        // SS220 TTS-Radio begin
        if (TryComp(parent, out ActorComponent? actor))
        {
            _netMan.ServerSendMessage(args.ChatMsg, actor.PlayerSession.Channel);

            if (parent != args.MessageSource && TryComp(args.MessageSource, out TTSComponent? _))
            {
                args.Receivers.Add(new(parent));
            }
        }
        // SS220 TTS-Radio end
    }

    // SS220 languages begin
    private void OnSendLangaugeMessageAttempt(Entity<HeadsetComponent> ent, ref SendLanguageMessageAttemptEvent args)
    {
        var actorUid = Transform(ent).ParentUid;
        if (HasComp<ActorComponent>(actorUid))
            RaiseLocalEvent(actorUid, ref args);
    }
    // SS220 languages end
}
