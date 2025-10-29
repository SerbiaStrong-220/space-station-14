using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Content.Shared.SS220.Language.Systems;
using Content.Shared.SS220.VoiceRangeModify; // ss220 add whisper range modify

namespace Content.Server.Speech.EntitySystems;

/// <summary>
///     This system redirects local chat messages to listening entities (e.g., radio microphones).
/// </summary>
public sealed class ListeningSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _xforms = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EntitySpokeEvent>(OnSpeak);
    }

    private void OnSpeak(EntitySpokeEvent ev)
    {
        PingListeners(ev.Source, ev.Message, ev.ObfuscatedMessage, ev.LanguageMessage /* SS220 languages*/);
    }

    public void PingListeners(EntityUid source, string message, string? obfuscatedMessage, LanguageMessage? languageMessage = null /* SS220 languages*/)
    {
        // TODO whispering / audio volume? Microphone sensitivity?
        // for now, whispering just arbitrarily reduces the listener's max range.

        var xformQuery = GetEntityQuery<TransformComponent>();
        var sourceXform = xformQuery.GetComponent(source);
        var sourcePos = _xforms.GetWorldPosition(sourceXform, xformQuery);

        var attemptEv = new ListenAttemptEvent(source);
        var ev = new ListenEvent(message, source, languageMessage: languageMessage /* SS220 languages*/);
        var obfuscatedEv = obfuscatedMessage == null ? null : new ListenEvent(obfuscatedMessage, source, true /* SS220 languages*/, languageMessage /* SS220 languages*/);
        var query = EntityQueryEnumerator<ActiveListenerComponent, TransformComponent>();

        // ss220 add whisper range modify start
        while(query.MoveNext(out var listenerUid, out _, out var xform))
        {
            if (xform.MapID != sourceXform.MapID)
                continue;

            // ss220 add whisper range modify start
            var modifyWhisperEv = new WhisperModifyRangeEvent(SharedChatSystem.WhisperClearRange, SharedChatSystem.WhisperMuffledRange);
            RaiseLocalEvent(listenerUid, ref modifyWhisperEv, true);
            var whisperClearRange = modifyWhisperEv.WhisperClearRange;

            var modifyVoiceRangeEv = new VoiceModifyRangeEvent(SharedChatSystem.VoiceRange);
            RaiseLocalEvent(listenerUid, ref modifyVoiceRangeEv, true);
            var voiceRange = modifyVoiceRangeEv.VoiceRange;

            // range checks
            // TODO proper speech occlusion
            var distance = (sourcePos - _xforms.GetWorldPosition(xform, xformQuery)).LengthSquared();
            if (distance > voiceRange * voiceRange)
                continue;

            RaiseLocalEvent(listenerUid, attemptEv);
            if (attemptEv.Cancelled)
            {
                attemptEv.Uncancel();
                continue;
            }

            if (obfuscatedEv != null && distance > whisperClearRange)
                RaiseLocalEvent(listenerUid, obfuscatedEv);
            else
                RaiseLocalEvent(listenerUid, ev);

            // ss220 add whisper range modify end
        }
    }
}
