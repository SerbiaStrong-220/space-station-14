// SS220 Changeling
using System.Linq;
using Content.Server.Changeling.Components;
using Content.Server.Polymorph.Components;
using Content.Server.Speech.Components;
using Content.Server.SS220.TTS;
using Content.Shared.Actions.Components;
using Content.Shared.Changeling;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Mutations;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Polymorph;
using Content.Shared.SS220.Telepathy;
using Content.Shared.SS220.TTS;
using Content.Shared.Store.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Changeling.Systems;

public sealed partial class ChangelingUtilityMutationSystem
{
    private void InitializeForms()
    {
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingHivemindActionEvent>(OnHivemind);
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingLesserFormActionEvent>(OnLesserForm);
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingMimicVoiceActionEvent>(OnMimicVoice);
        SubscribeLocalEvent<PolymorphedEntityComponent, ChangelingHumanFormActionEvent>(OnHumanForm);
        SubscribeLocalEvent<PolymorphedEntityComponent, PolymorphedEvent>(OnLesserFormReverted);
    }

    private void OnHivemind(Entity<ChangelingResourceComponent> ent, ref ChangelingHivemindActionEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner || !Spend(ent.Owner, 10))
            return;

        var state = EnsureState(ent);
        TelepathyComponent telepathy;
        if (TryComp<TelepathyComponent>(ent, out var existingTelepathy))
        {
            telepathy = existingTelepathy;
            if (!state.AddedTelepathy && !state.OriginalTelepathyCaptured)
            {
                state.OriginalTelepathyCaptured = true;
                state.OriginalTelepathyCanSend = telepathy.CanSend;
                state.OriginalTelepathyChannel = telepathy.TelepathyChannelPrototype;
                state.OriginalTelepathyReceiveAllChannels = telepathy.ReceiveAllChannels;
            }
        }
        else
        {
            telepathy = AddComp<TelepathyComponent>(ent);
            state.AddedTelepathy = true;
        }

        telepathy.CanSend = true;
        telepathy.TelepathyChannelPrototype = HiveTelepathyChannel;
        Dirty(ent.Owner, telepathy);
        args.Handled = true;
        SynchronizeHiveGenomes();
        _popup.PopupEntity(Loc.GetString("changeling-hivemind-synchronized"), ent.Owner, ent.Owner);
    }

    private void RestoreTelepathy(EntityUid uid, ChangelingUtilityStateComponent state)
    {
        if (state.AddedTelepathy)
        {
            RemComp<TelepathyComponent>(uid);
        }
        else if (state.OriginalTelepathyCaptured && TryComp<TelepathyComponent>(uid, out var telepathy))
        {
            telepathy.CanSend = state.OriginalTelepathyCanSend;
            telepathy.TelepathyChannelPrototype = state.OriginalTelepathyChannel;
            telepathy.ReceiveAllChannels = state.OriginalTelepathyReceiveAllChannels;
            Dirty(uid, telepathy);
        }

        state.AddedTelepathy = false;
        state.OriginalTelepathyCaptured = false;
        state.OriginalTelepathyChannel = null;
    }

    private void SynchronizeHiveGenomes()
    {
        _hiveGenomes.Clear();
        var query = EntityQueryEnumerator<ChangelingIdentityComponent, TelepathyComponent>();
        while (query.MoveNext(out _, out var identity, out var telepathy))
        {
            if (telepathy.TelepathyChannelPrototype != HiveTelepathyChannel)
                continue;

            foreach (var (sample, genome) in identity.StoredGenomes)
            {
                if (Exists(sample))
                    _hiveGenomes.TryAdd(genome, sample);
            }
        }

        query = EntityQueryEnumerator<ChangelingIdentityComponent, TelepathyComponent>();
        while (query.MoveNext(out var uid, out var identity, out var telepathy))
        {
            if (telepathy.TelepathyChannelPrototype != HiveTelepathyChannel)
                continue;

            foreach (var (genome, sample) in _hiveGenomes)
            {
                if (identity.ConsumedIdentities.Count >= identity.MaxStoredIdentities)
                    break;

                _identities.TryStoreIdentity((uid, identity),
                    sample,
                    genome,
                    null,
                    countForObjective: false,
                    out _);
            }
        }
    }

    private void OnLesserForm(Entity<ChangelingResourceComponent> ent, ref ChangelingLesserFormActionEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner)
            return;

        if (FindPurchasedAction(ent.Owner, HumanFormAction) is not { } humanFormAction)
        {
            _popup.PopupEntity(Loc.GetString("changeling-lesser-form-requires-human-form"), ent.Owner, ent.Owner);
            return;
        }

        if (!Spend(ent.Owner, 5))
            return;

        var cleanup = new ChangelingEvolutionResetEvent(ent.Comp.EvolutionPoints, ent.Comp.EvolutionPoints);
        RaiseLocalEvent(ent.Owner, ref cleanup);
        UntogglePurchasedActions(ent.Owner);

        if (_polymorph.PolymorphEntity(ent.Owner, ChangelingLesserForm) is not { } child)
        {
            _resources.AddChemicals(ent.Owner, FixedPoint2.New(5));
            return;
        }

        EnsureComp<ChangelingLesserFormComponent>(child);

        _actionContainer.TransferActionWithNewAttached(humanFormAction, child, child);
        if (_actions.GetAction(humanFormAction) is not { } action || action.Comp.AttachedEntity != child)
        {
            _polymorph.Revert((child, Comp<PolymorphedEntityComponent>(child)));
            _resources.AddChemicals(ent.Owner, FixedPoint2.New(5));
            return;
        }

        args.Handled = true;
        _popup.PopupEntity(Loc.GetString("changeling-lesser-form-entered"), child, child);
    }

    private void OnHumanForm(Entity<PolymorphedEntityComponent> ent, ref ChangelingHumanFormActionEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner || ent.Comp.Parent is not { } parent)
            return;

        if (!_resources.TrySpendChemicals(parent, FixedPoint2.New(5)))
        {
            _popup.PopupEntity(Loc.GetString("changeling-not-enough-chemicals"), ent.Owner, ent.Owner);
            return;
        }

        if (_polymorph.Revert(ent.AsNullable()) is not { } restoredParent)
        {
            _resources.AddChemicals(parent, FixedPoint2.New(5));
            return;
        }

        if (!TryReturnHumanFormAction(restoredParent, ent.Owner))
            Log.Error($"Failed to return Human Form action to {ToPrettyString(restoredParent)} after polymorph reversion.");

        args.Handled = true;
        _popup.PopupEntity(Loc.GetString("changeling-human-form-restored"), restoredParent, restoredParent);
    }

    private void OnLesserFormReverted(Entity<PolymorphedEntityComponent> ent, ref PolymorphedEvent args)
    {
        if (!args.IsRevert || args.OldEntity != ent.Owner)
            return;

        if (FindPurchasedAction(args.NewEntity, HumanFormAction) is not { } action ||
            _actions.GetAction(action) is not { } actionData ||
            actionData.Comp.AttachedEntity != ent.Owner)
        {
            return;
        }

        if (!TryReturnHumanFormAction(args.NewEntity, ent.Owner))
            Log.Error($"Failed to preserve Human Form action while reverting {ToPrettyString(ent.Owner)}.");
    }

    private bool TryReturnHumanFormAction(EntityUid parent, EntityUid expectedCurrentOwner)
    {
        if (FindPurchasedAction(parent, HumanFormAction) is not { } action ||
            _actions.GetAction(action) is not { } actionData)
        {
            return false;
        }

        if (actionData.Comp.AttachedEntity == parent)
            return true;
        if (actionData.Comp.AttachedEntity != expectedCurrentOwner)
            return false;

        _actionContainer.TransferActionWithNewAttached(action, parent, parent);
        return _actions.GetAction(action) is { } transferred && transferred.Comp.AttachedEntity == parent;
    }

    private void UntogglePurchasedActions(EntityUid uid)
    {
        if (!TryComp<StoreComponent>(uid, out var store))
            return;

        foreach (var action in store.BoughtEntities)
        {
            if (Exists(action) && HasComp<ActionComponent>(action))
                _actions.SetToggled(action, false);
        }
    }

    private EntityUid? FindPurchasedAction(EntityUid uid, EntProtoId prototype)
    {
        if (!TryComp<StoreComponent>(uid, out var store))
            return null;

        foreach (var action in store.BoughtEntities)
        {
            if (!Exists(action) ||
                !HasComp<ActionComponent>(action) ||
                MetaData(action).EntityPrototype?.ID != prototype.Id)
            {
                continue;
            }

            return action;
        }

        return null;
    }

    private void OnMimicVoice(Entity<ChangelingResourceComponent> ent, ref ChangelingMimicVoiceActionEvent args)
    {
        if (args.Handled ||
            args.Performer != ent.Owner ||
            !TryComp<ChangelingIdentityComponent>(ent, out var identity))
            return;

        args.Handled = true;
        var samples = identity.ConsumedIdentities.Keys.Where(Exists).ToArray();
        var state = EnsureState(ent);
        if (state.MimicVoiceIndex == -1 && !state.MimicVoiceCaptured)
        {
            state.MimicVoiceCaptured = true;
            state.HadOriginalTts = TryComp<TTSComponent>(ent, out var originalTts);
            state.OriginalVoice = originalTts?.VoicePrototypeId;
            if (TryComp<VoiceOverrideComponent>(ent, out var originalOverride))
            {
                state.OriginalVoiceOverrideCaptured = true;
                state.OriginalVoiceOverrideName = originalOverride.NameOverride;
                state.OriginalSpeechVerbOverride = originalOverride.SpeechVerbOverride;
                state.OriginalVoiceOverrideEnabled = originalOverride.Enabled;
                state.OriginalLastSetVoice = originalOverride.LastSetVoice;
            }
        }

        state.MimicVoiceIndex++;
        if (samples.Length == 0 || state.MimicVoiceIndex >= samples.Length)
        {
            state.MimicVoiceIndex = -1;
            RestoreOriginalVoice(ent.Owner, state);
            _popup.PopupEntity(Loc.GetString("changeling-mimic-voice-restored"), ent.Owner, ent.Owner);
            return;
        }

        var sample = samples[state.MimicVoiceIndex];
        VoiceOverrideComponent voice;
        if (!TryComp<VoiceOverrideComponent>(ent, out var existingOverride))
        {
            voice = AddComp<VoiceOverrideComponent>(ent);
            state.AddedVoiceOverride = true;
        }
        else
        {
            voice = existingOverride;
        }

        voice.NameOverride = Identity.Name(sample, EntityManager);
        voice.Enabled = true;
        if (TryComp<TTSComponent>(sample, out var targetTts))
            _tts.TrySetTTS(ent.Owner, targetTts.VoicePrototypeId);
        else
            RemComp<TTSComponent>(ent);

        _popup.PopupEntity(
            Loc.GetString("changeling-mimic-voice-set", ("name", Identity.Name(sample, EntityManager))),
            ent.Owner,
            ent.Owner);
    }

    private void RestoreOriginalVoice(EntityUid uid, ChangelingUtilityStateComponent state)
    {
        if (!state.MimicVoiceCaptured)
            return;

        if (state.HadOriginalTts)
        {
            var tts = EnsureComp<TTSComponent>(uid);
            tts.VoicePrototypeId = state.OriginalVoice;
            Dirty(uid, tts);
        }
        else
        {
            RemComp<TTSComponent>(uid);
        }

        if (state.AddedVoiceOverride)
        {
            RemComp<VoiceOverrideComponent>(uid);
        }
        else if (state.OriginalVoiceOverrideCaptured && TryComp<VoiceOverrideComponent>(uid, out var voiceOverride))
        {
            voiceOverride.NameOverride = state.OriginalVoiceOverrideName;
            voiceOverride.SpeechVerbOverride = state.OriginalSpeechVerbOverride;
            voiceOverride.Enabled = state.OriginalVoiceOverrideEnabled;
            voiceOverride.LastSetVoice = state.OriginalLastSetVoice;
            Dirty(uid, voiceOverride);
        }

        state.MimicVoiceCaptured = false;
        state.HadOriginalTts = false;
        state.OriginalVoice = null;
        state.AddedVoiceOverride = false;
        state.OriginalVoiceOverrideCaptured = false;
        state.OriginalVoiceOverrideName = null;
        state.OriginalSpeechVerbOverride = null;
        state.OriginalLastSetVoice = null;
    }
}
