using Content.Server.Chat.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Speech;
using Content.Server.Speech.Components;
using Content.Server.SS220.Language;
using Content.Server.SS220.TTS; // SS220 Tape recorder TTS
using Content.Shared.Chat;
using Content.Shared.Paper;
using Content.Shared.Speech;
using Content.Shared.SS220.TTS; // SS220 Tape recorder TTS
using Content.Shared.TapeRecorder;
using Content.Shared.TapeRecorder.Components;
using Content.Shared.TapeRecorder.Events;
using Robust.Server.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Text;

namespace Content.Server.TapeRecorder;

public sealed class TapeRecorderSystem : SharedTapeRecorderSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly TTSSystem _ttsSystem = default!; // SS220 Tape recorder TTS
    [Dependency] private readonly LanguageSystem _languageSystem = default!; // SS220 languages

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TapeRecorderComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<TapeRecorderComponent, PrintTapeRecorderMessage>(OnPrintMessage);
    }

    /// <summary>
    /// Given a time range, play all messages on a tape within said range, [start, end).
    /// Split into this system as shared does not have ChatSystem access
    /// </summary>
    protected override void ReplayMessagesInSegment(Entity<TapeRecorderComponent> ent, TapeCassetteComponent tape, float segmentStart, float segmentEnd)
    {
        var voice = EnsureComp<VoiceOverrideComponent>(ent);
        var speech = EnsureComp<SpeechComponent>(ent);
        TryComp<TTSComponent>(ent, out var tts); // SS220 Tape recorder TTS

        foreach (var message in tape.RecordedData)
        {
            if (message.Timestamp < tape.CurrentPosition || message.Timestamp >= segmentEnd)
                continue;

            //Change the voice to match the speaker
            voice.NameOverride = message.Name ?? ent.Comp.DefaultName;
            // TODO: mimic the exact string chosen when the message was recorded
            var verb = message.Verb ?? SharedChatSystem.DefaultSpeechVerb;
            speech.SpeechVerb = _proto.Index<SpeechVerbPrototype>(verb);
            // SS220 Tape recorder TTS begin
            if (tts is { })
            {
                tts.VoicePrototypeId = message.TtsVoice;
            }
            // SS220 Tape recorder TTS end
            //Play the message

            // SS220 languages begin
            //_chat.TrySendInGameICMessage(ent, message.Message, InGameICChatType.Speak, false);

            if (message.LanguageMessage is { } languageMessage)
                _chat.TrySendInGameICMessage(ent, languageMessage.GetMessageWithLanguageKeys(), InGameICChatType.Speak, false);
            else
                _chat.TrySendInGameICMessage(ent, message.Message, InGameICChatType.Speak, false);
            // SS220 languages end
        }
    }

    /// <summary>
    /// Whenever someone speaks within listening range, record it to tape
    /// </summary>
    private void OnListen(Entity<TapeRecorderComponent> ent, ref ListenEvent args)
    {
        // mode should never be set when it isn't active but whatever
        if (ent.Comp.Mode != TapeRecorderMode.Recording || !HasComp<ActiveTapeRecorderComponent>(ent))
            return;

        // No feedback loops
        if (args.Source == ent.Owner)
            return;

        if (!TryGetTapeCassette(ent, out var cassette))
            return;

        // TODO: Handle "Someone" when whispering from far away, needs chat refactor

        //Handle someone using a voice changer
        var nameEv = new TransformSpeakerNameEvent(args.Source, Name(args.Source));
        RaiseLocalEvent(args.Source, nameEv);

        //Add a new entry to the tape
        var verb = _chat.GetSpeechVerb(args.Source, args.Message);
        var name = nameEv.VoiceName;
        // SS220 Tape recorder TTS begin
        //cassette.Comp.Buffer.Add(new TapeCassetteRecordedMessage(cassette.Comp.CurrentPosition, name, verb, args.Message));
        TryComp<TTSComponent>(args.Source, out var tts);
        var voiceId = tts?.VoicePrototypeId;
        if (voiceId is { } && _ttsSystem.TryGetVoiceMaskUid(args.Source, out var maskUid))
        {
            var voiceEv = new TransformSpeakerVoiceEvent(maskUid.Value, voiceId);
            RaiseLocalEvent(maskUid.Value, voiceEv);
            voiceId = voiceEv.VoiceId;
        }
        cassette.Comp.Buffer.Add(new TapeCassetteRecordedMessage(cassette.Comp.CurrentPosition, name, verb, args.Message, voiceId, args.LanguageMessage));
        // SS220 Tape recorder TTS end
    }

    private void OnPrintMessage(Entity<TapeRecorderComponent> ent, ref PrintTapeRecorderMessage args)
    {
        var (uid, comp) = ent;

        if (comp.CooldownEndTime > Timing.CurTime)
            return;

        if (!TryGetTapeCassette(ent, out var cassette))
            return;

        var text = new StringBuilder();
        var paper = Spawn(comp.PaperPrototype, Transform(ent).Coordinates);

        // Sorting list by time for overwrite order
        // TODO: why is this needed? why wouldn't it be stored in order
        var data = cassette.Comp.RecordedData;
        data.Sort((x,y) => x.Timestamp.CompareTo(y.Timestamp));

        // Looking if player's entity exists to give paper in its hand
        var player = args.Actor;
        if (Exists(player))
            _hands.PickupOrDrop(player, paper, checkActionBlocker: false);

        if (!TryComp<PaperComponent>(paper, out var paperComp))
            return;

        Audio.PlayPvs(comp.PrintSound, ent);

        text.AppendLine(Loc.GetString("tape-recorder-print-start-text"));
        text.AppendLine();
        foreach (var message in cassette.Comp.RecordedData)
        {
            var name = message.Name ?? ent.Comp.DefaultName;
            var time = TimeSpan.FromSeconds((double) message.Timestamp);

            // SS220 languages begin
            var recordedMessage = "";
            if (message.LanguageMessage is { } languageMessage)
            {
                for (var i = 0; i < languageMessage.Nodes.Count; i++)
                {
                    var node = languageMessage.Nodes[i];
                    if (i != 0)
                        recordedMessage += " ";

                    recordedMessage += _languageSystem.GenerateLanguageMsgMarkup(node.Message, node.Language);
                }
            }
            else
                recordedMessage = message.Message;
            // SS220 languages end

            text.AppendLine(Loc.GetString("tape-recorder-print-message-text",
                ("time", time.ToString(@"hh\:mm\:ss")),
                ("source", name),
                ("message", recordedMessage))); // SS220 languages
        }
        text.AppendLine();
        text.Append(Loc.GetString("tape-recorder-print-end-text"));

        _paper.SetContent((paper, paperComp), text.ToString());

        comp.CooldownEndTime = Timing.CurTime + comp.PrintCooldown;
    }
}
