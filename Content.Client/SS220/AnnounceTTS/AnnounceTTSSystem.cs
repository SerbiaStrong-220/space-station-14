using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.SS220.AnnounceTTS;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;

namespace Content.Client.SS220.AnnounceTTS;

// ReSharper disable once InconsistentNaming
public sealed class AnnounceTTSSystem : EntitySystem
{
    [Dependency] private readonly IClydeAudio _clyde = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IEyeManager _eye = default!;

    private ISawmill _sawmill = default!;
    private float _volume = 0.0f;

    private readonly HashSet<AudioStream> _currentStreams = new();
    private readonly Dictionary<int, Queue<AudioStream>> _entityQueues = new();

    /// <inheritdoc />
    public override void Initialize()
    {
        _sawmill = Logger.GetSawmill("AnnounceTTSSystem");
        _cfg.OnValueChanged(CCCVars.TTSVolume, OnTtsVolumeChanged, true);
        SubscribeNetworkEvent<AnnounceTTSEvent>(OnAnnounceTTSPlay);
    }

    /// <inheritdoc />
    public override void FrameUpdate(float frameTime)
    {
        var streamToRemove = new HashSet<AudioStream>();

        foreach (var stream in _currentStreams.Where(stream => !stream.Source.IsPlaying))
        {
            stream.Source.Dispose();
            streamToRemove.Add(stream);
        }

        foreach (var audioStream in streamToRemove)
        {
            _currentStreams.Remove(audioStream);
            ProcessEntityQueue(audioStream.Id);
        }
    }

    /// <inheritdoc />
    public override void Shutdown()
    {
        _cfg.UnsubValueChanged(CCCVars.TTSVolume, OnTtsVolumeChanged);
        EndStreams();
    }

    private void OnAnnounceTTSPlay(AnnounceTTSEvent ev)
    {
        var volume = _volume;
        if (!TryCreateAudioSource(ev.Data, volume, out var source))
            return;

        var stream = new AudioStream(ev.Id, source);
        AddEntityStreamToQueue(stream);
    }

    private void AddEntityStreamToQueue(AudioStream stream)
    {
        if (_entityQueues.TryGetValue(stream.Id, out var queue))
        {
            queue.Enqueue(stream);
        }
        else
        {
            _entityQueues.Add(stream.Id, new Queue<AudioStream>(new[] { stream }));

            if (!IsEntityCurrentlyPlayStream(stream.Id))
                ProcessEntityQueue(stream.Id);
        }
    }

    private bool IsEntityCurrentlyPlayStream(int id)
    {
        return _currentStreams.Any(s => s.Id == id);
    }

    private void ProcessEntityQueue(int id)
    {
        if (TryTakeEntityStreamFromQueue(id, out var stream))
            PlayEntity(stream);
    }
    private bool TryTakeEntityStreamFromQueue(int id, [NotNullWhen(true)] out AudioStream? stream)
    {
        if (_entityQueues.TryGetValue(id, out var queue))
        {
            stream = queue.Dequeue();
            if (queue.Count == 0)
                _entityQueues.Remove(id);
            return true;
        }

        stream = null;
        return false;
    }

    private bool TryCreateAudioSource(byte[] data, float volume, [NotNullWhen(true)] out IClydeAudioSource? source)
    {
        var dataStream = new MemoryStream(data) { Position = 0 };
        var audioStream = _clyde.LoadAudioOggVorbis(dataStream);
        source = _clyde.CreateAudioSource(audioStream);
        source?.SetMaxDistance(float.MaxValue);
        source?.SetReferenceDistance(1f);
        source?.SetRolloffFactor(1f);
        source?.SetVolume(volume);
        return source != null;
    }

    private void PlayEntity(AudioStream stream)
    {
        if(!stream.Source.SetPosition(_eye.CurrentEye.Position.Position))
            return;

        stream.Source.StartPlaying();
        _currentStreams.Add(stream);
    }

    private void OnTtsVolumeChanged(float volume)
    {
        _volume = volume;
    }

    private void EndStreams()
    {
        foreach (var stream in _currentStreams)
        {
            stream.Source.StopPlaying();
            stream.Source.Dispose();
        }

        _currentStreams.Clear();
        _entityQueues.Clear();
    }

    // ReSharper disable once InconsistentNaming
    private sealed class AudioStream
    {
        public int Id { get; }
        public IClydeAudioSource Source { get; }

        public AudioStream(int id, IClydeAudioSource source)
        {
            Id = id;
            Source = source;
        }
    }
}
