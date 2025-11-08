// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.IO;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.TTS;
using Content.Shared.SS220.TTS.Commands;
using Robust.Client.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Client.SS220.TTS;

/// <summary>
/// Plays TTS audio in world
/// </summary>
// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem
{
    [Dependency] private readonly IAudioManager _audioManager = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly TTSManager _ttsManager = default!;

    private ISawmill _sawmill = default!;

    /// <summary>
    /// Reducing the volume of the TTS when whispering. Will be converted to logarithm.
    /// </summary>
    private const float WhisperFade = 4f;

    private float _volume = 0.0f;
    private float _radioVolume = 0.0f;

    private int _maxQueuedPerEntity = 20;
    private int _maxEntitiesQueued = 30;

    private readonly Dictionary<TtsMetadata, Queue<PlayRequest>> _playQueues = new();
    private readonly Dictionary<TtsMetadata, EntityUid?> _playingStreams = new();

    private readonly EntityUid _fakeRecipient = new();

    public override void Initialize()
    {
        _sawmill = Logger.GetSawmill("tts");

        // remove if Robust PR for clientCVar subs merged
        _cfg.OnValueChanged(CCVars220.RecieveTTS, x => RaiseNetworkEvent(new SessionSendTTSMessage(x)), true);
        //end

        Subs.CVar(_cfg, CCVars220.MaxQueuedPerEntity, (x) => _maxQueuedPerEntity = x, true);
        Subs.CVar(_cfg, CCVars220.MaxEntitiesQueued, (x) => _maxEntitiesQueued = x, true);
        _cfg.OnValueChanged(CCCVars.TTSVolume, OnTtsVolumeChanged, true);
        _cfg.OnValueChanged(CCCVars.TTSRadioVolume, OnTtsRadioVolumeChanged, true);

        SubscribeNetworkEvent<TtsQueueResetMessage>(OnQueueResetRequest);

        _ttsManager.PlayTtsReceived += OnPlayTts;

        InitializeAnnounces();
        InitializeMetadata();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(CCCVars.TTSVolume, OnTtsVolumeChanged);
        _cfg.UnsubValueChanged(CCCVars.TTSRadioVolume, OnTtsRadioVolumeChanged);

        _ttsManager.PlayTtsReceived -= OnPlayTts;

        ShutdownAnnounces();
        ResetQueuesAndEndStreams();
    }

    public void RequestGlobalTTS(string text, string voiceId)
    {
        RaiseNetworkEvent(new RequestGlobalTTSEvent(text, voiceId));
    }

    private void OnTtsVolumeChanged(float volume)
    {
        _volume = volume;
    }

    private void OnTtsRadioVolumeChanged(float volume)
    {
        _radioVolume = volume;
    }

    private void OnQueueResetRequest(TtsQueueResetMessage ev)
    {
        ResetQueuesAndEndStreams();
        _sawmill.Debug("TTS queue was cleared by request from the server.");
    }

    public void ResetQueuesAndEndStreams()
    {
        foreach (var key in _playingStreams.Keys)
        {
            _playingStreams[key] = _audio.Stop(_playingStreams[key]);
        }

        _playingStreams.Clear();
        _playQueues.Clear();
    }

    // Process sound queues on frame update
    public override void FrameUpdate(float frameTime)
    {
        var streamsToRemove = new HashSet<TtsMetadata>();

        foreach (var (metadata, stream) in _playingStreams)
        {
            if (!TryComp(stream, out AudioComponent? _))
            {
                streamsToRemove.Add(metadata);
            }
        }

        foreach (var metadata in streamsToRemove)
        {
            _playingStreams.Remove(metadata);
        }

        var queueUidsToRemove = new HashSet<TtsMetadata>();

        foreach (var (metadata, queue) in _playQueues)
        {
            if (_playingStreams.ContainsKey(metadata))
                continue;

            if (!queue.TryDequeue(out var request))
                continue;

            if (queue.Count == 0)
                queueUidsToRemove.Add(metadata);

            AudioStream? audioStream;
            (EntityUid Entity, AudioComponent Component)? stream;
            switch (request)
            {
                case PlayRequestByAudioStream playRequestByAudio:
                    audioStream = playRequestByAudio.AudioStream;

                    if (request.PlayGlobal)
                        stream = _audio.PlayGlobal(audioStream, null, request.Params);
                    else
                        stream = _audio.PlayEntity(audioStream, request.Source, null, request.Params);
                    break;

                case PlayRequestBySoundSpecifier playRequestBySoundSpecifier:
                    if (request.PlayGlobal)
                        stream = _audio.PlayGlobal(playRequestBySoundSpecifier.Sound, Filter.Local(), false);
                    else
                        stream = _audio.PlayEntity(playRequestBySoundSpecifier.Sound, _fakeRecipient, request.Source);
                    break;

                default:
                    continue;
            }

            if (stream.HasValue && stream.Value.Component is not null)
            {
                _playingStreams.Add(metadata, stream.Value.Entity);
            }
        }

        foreach (var queueMetadata in queueUidsToRemove)
        {
            _playQueues.Remove(queueMetadata);
        }
    }

    public void TryQueueRequest(TtsMetadata metadata, PlayRequest request)
    {
        ModifyMetadata(ref metadata, request.Source);

        if (!_playQueues.TryGetValue(metadata, out var queue))
        {
            if (_playQueues.Count >= _maxEntitiesQueued)
                return;

            queue = new();
            _playQueues.Add(metadata, queue);
        }

        if (queue.Count >= _maxQueuedPerEntity)
            return;

        queue.Enqueue(request);
    }

    public void TryQueuePlayByAudioStream(EntityUid entity, AudioStream audioStream, TtsMetadata metadata, AudioParams audioParams, bool globally = false)
    {
        var request = new PlayRequestByAudioStream(audioStream, entity, audioParams, globally);
        TryQueueRequest(metadata, request);
    }

    private void PlaySoundQueued(EntityUid entity, SoundSpecifier sound, TtsMetadata metadata, bool globally = false)
    {
        var request = new PlayRequestBySoundSpecifier(sound, entity, globally);
        TryQueueRequest(metadata, request);
    }

    private void QueuePlayTts(TtsAudioData data, TtsMetadata metadata, EntityUid? sourceUid = null, AudioParams? audioParams = null, bool globally = false)
    {
        if (data.Length == 0)
            return;

        var finalParams = audioParams ?? AudioParams.Default;

        using MemoryStream stream = new(data.Buffer);
        var audioStream = _audioManager.LoadAudioOggVorbis(stream);

        if (sourceUid == null)
        {
            _audio.PlayGlobal(audioStream, null);
        }
        else
        {
            if (sourceUid.HasValue && sourceUid.Value.IsValid())
                TryQueuePlayByAudioStream(sourceUid.Value, audioStream, metadata, finalParams, globally);
        }
    }

    private void OnPlayTts(MsgPlayTts msg)
    {
        var volume = AdjustVolume(msg.Metadata.Kind);
        var audioParams = AudioParams.Default.WithVolume(volume);

        QueuePlayTts(msg.Data, msg.Metadata, GetEntity(msg.SourceUid), audioParams, msg.Metadata.Kind == TtsKind.Telepathy);
    }

    private float AdjustVolume(TtsKind kind)
    {
        var volume = kind switch
        {
            TtsKind.Radio => _radioVolume,
            TtsKind.Announce => VolumeAnnounce,
            _ => _volume,
        };

        volume = SharedAudioSystem.GainToVolume(volume);

        if (kind == TtsKind.Whisper)
        {
            volume -= SharedAudioSystem.GainToVolume(WhisperFade);
        }

        return volume;
    }

    // Play requests //
    public abstract class PlayRequest
    {
        public readonly AudioParams Params = AudioParams.Default;
        public readonly bool PlayGlobal = false;
        public readonly EntityUid Source;

        public PlayRequest(EntityUid? source = null, AudioParams? audioParams = null, bool playGlobal = false)
        {
            Source = source ?? EntityUid.FirstUid;
            PlayGlobal = playGlobal;
            if (audioParams.HasValue)
                Params = audioParams.Value;
        }
    }

    public sealed class PlayRequestByAudioStream : PlayRequest
    {
        public readonly AudioStream AudioStream;

        public PlayRequestByAudioStream(AudioStream audioStream, EntityUid? source = null, AudioParams? audioParams = null, bool playGlobal = false) : base(source, audioParams, playGlobal)
        {
            AudioStream = audioStream;
        }
    }

    public sealed class PlayRequestBySoundSpecifier : PlayRequest
    {
        public readonly SoundSpecifier Sound;

        public PlayRequestBySoundSpecifier(SoundSpecifier sound, EntityUid? source = null,  bool playGlobal = false) : base(source, sound.Params, playGlobal)
        {
            Sound = sound;
        }
    }
}
