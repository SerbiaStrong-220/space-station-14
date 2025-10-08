using Content.Server.Chat.Systems;
using Content.Shared.Chat; // ss220 add whisper range modify
using Content.Shared.Humanoid; // ss220 add whisper range modify
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Content.Shared.SS220.Language.Systems;
using Robust.Shared.Prototypes; // ss220 add whisper range modify

namespace Content.Server.Speech.EntitySystems;

/// <summary>
///     This system redirects local chat messages to listening entities (e.g., radio microphones).
/// </summary>
public sealed class ListeningSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!; // ss220 add whisper range modify
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

        while(query.MoveNext(out var listenerUid, out var listener, out var xform))
        {
            if (xform.MapID != sourceXform.MapID)
                continue;

            // range checks
            // TODO proper speech occlusion
            var distance = (sourcePos - _xforms.GetWorldPosition(xform, xformQuery)).LengthSquared();
            if (distance > listener.Range * listener.Range)
                continue;

            RaiseLocalEvent(listenerUid, attemptEv);
            if (attemptEv.Cancelled)
            {
                attemptEv.Uncancel();
                continue;
            }

            // ss220 add whisper range modify start
            var whisperClearRange = (float)SharedChatSystem.WhisperClearRange;
            if (TryComp<HumanoidAppearanceComponent>(listenerUid, out var huAp))
            {
                var species = _proto.Index(huAp.Species);
                whisperClearRange = species.BaseWhisperClearRange;
            }

            if (obfuscatedEv != null && distance > whisperClearRange)
                RaiseLocalEvent(listenerUid, obfuscatedEv);
            else
                RaiseLocalEvent(listenerUid, ev);

            // ss220 add whisper range modify end
        }
    }
}
