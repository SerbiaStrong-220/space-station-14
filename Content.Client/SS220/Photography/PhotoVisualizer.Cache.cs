// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Stopwatch = System.Diagnostics.Stopwatch;
using Content.Shared.GameTicking;
using Content.Shared.SS220.Photography;
using Robust.Shared.Timing;

namespace Content.Client.SS220.Photography;

public sealed partial class PhotoVisualizer
{
    [Dependency] private IGameTiming _timing = default!;

    private static readonly TimeSpan VisualizationBudget = TimeSpan.FromMilliseconds(2);
    private static readonly TimeSpan RequestInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

    private readonly Dictionary<string, HashSet<PhotoEyeRequest>> _eyeRequests = new();
    private readonly Queue<PhotoEyeRequest> _eyeRequestQueue = new();
    private readonly Dictionary<string, PhotoData> _photoDataCache = new();
    private PhotoEyeRequest? _activeRequest;
    private IEnumerator<bool>? _loadingVisualization;
    private bool _visualizationReady;
    private TimeSpan _nextRequestTime;

    private void InitializeCache()
    {
        SubscribeNetworkEvent<PhotoDataRequestResponse>(OnPhotoDataReceived);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    public override void Shutdown()
    {
        CleanupRequests();
        base.Shutdown();
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent args)
    {
        CleanupRequests();
        _reservedMap = null;
    }

    private void CleanupRequests()
    {
        _loadingVisualization?.Dispose();
        _loadingVisualization = null;
        _activeRequest = null;
        _visualizationReady = false;

        // Dispose callbacks modify the request sets. Detach them before invoking any callbacks.
        var requests = new List<PhotoEyeRequest>();
        foreach (var set in _eyeRequests.Values)
        {
            requests.AddRange(set);
        }

        _eyeRequests.Clear();
        _eyeRequestQueue.Clear();
        _photoDataCache.Clear();
        _nextRequestTime = TimeSpan.Zero;
        foreach (var request in requests)
        {
            request.Dispose();
        }

        DisposeVisualization();

        // The round is ending; do not leave client entities on the next round's reserved map.
        while (_cleanupEntities.TryDequeue(out var entity))
        {
            if (!Deleted(entity))
                Del(entity);
        }
    }

    private void OnPhotoDataReceived(PhotoDataRequestResponse args)
    {
        if (!_eyeRequests.ContainsKey(args.Id))
            return;

        if (args.Data is not { Valid: true } data || data.Id != args.Id)
        {
            // A denial applies to the requesting window, not other windows of a copied photo.
            if (_eyeRequestQueue.TryPeek(out var request) && request.PhotoId == args.Id)
                request.Dispose();
            return;
        }

        _photoDataCache.TryAdd(args.Id, data);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        var start = Stopwatch.GetTimestamp();

        while (_cleanupEntities.TryDequeue(out var entity))
        {
            if (!Deleted(entity))
                Del(entity);
            if (Stopwatch.GetElapsedTime(start) >= VisualizationBudget)
                return;
        }

        if (_activeRequest is { } active)
        {
            if (_loadingVisualization == null)
            {
                if (_visualizationReady)
                {
                    // Let deferred appearance/transform updates finish before the viewport draws.
                    _visualizationReady = false;
                    active.OnVisualizationInit?.Invoke(_sceneEye!,
                        _photoDataCache[active.PhotoId].PhotoSize);
                }
                return; // Wait until the viewport finishes copying its pixels.
            }

            while (Stopwatch.GetElapsedTime(start) < VisualizationBudget)
            {
                if (_loadingVisualization.MoveNext())
                    continue;

                _loadingVisualization.Dispose();
                _loadingVisualization = null;
                _visualizationReady = true;
                return;
            }

            return;
        }

        while (_eyeRequestQueue.TryPeek(out var disposed) && disposed.Disposed)
        {
            _eyeRequestQueue.Dequeue();
        }

        if (!_eyeRequestQueue.TryPeek(out var request))
            return;

        if (Deleted(request.Owner) ||
            request.RequestedAt is { } requested && _timing.RealTime - requested >= RequestTimeout)
        {
            request.Dispose();
            return;
        }

        if (!_photoDataCache.TryGetValue(request.PhotoId, out var data))
        {
            if (request.RequestedAt == null && _timing.RealTime >= _nextRequestTime)
            {
                request.RequestedAt = _timing.RealTime;
                _nextRequestTime = _timing.RealTime + RequestInterval;
                RaiseNetworkEvent(new PhotoDataRequest(request.PhotoId, GetNetEntity(request.Owner)));
            }
            return;
        }

        if (_reservedMap is not { } map || !_mapMan.MapExists(map))
            return;

        _eyeRequestQueue.Dequeue();
        _activeRequest = request;
        _loadingVisualization = CreateVisualization(data, map);
    }

    private void ForgetRequestAndCleanup(PhotoEyeRequest request)
    {
        if (_activeRequest == request)
        {
            _loadingVisualization?.Dispose();
            _loadingVisualization = null;
            _activeRequest = null;
            _visualizationReady = false;
            DisposeVisualization();
        }

        if (!_eyeRequests.TryGetValue(request.PhotoId, out var requests))
            return;

        requests.Remove(request);
        if (requests.Count != 0)
            return;

        _photoDataCache.Remove(request.PhotoId);
        _eyeRequests.Remove(request.PhotoId);
    }

    public void RequestPhotoEye(PhotoEyeRequest request)
    {
        if (request.Disposed || request.Cleanup != null)
            return;

        if (!_eyeRequests.TryGetValue(request.PhotoId, out var requests))
        {
            requests = new HashSet<PhotoEyeRequest>();
            _eyeRequests.Add(request.PhotoId, requests);
        }

        requests.Add(request);
        request.Cleanup = ForgetRequestAndCleanup;
        _eyeRequestQueue.Enqueue(request);
    }
}

public sealed class PhotoEyeRequest(EntityUid owner, string photoId) : IDisposable
{
    public readonly EntityUid Owner = owner;
    public readonly string PhotoId = photoId;
    public Action<EyeComponent, float>? OnVisualizationInit;
    public Action? OnDispose;
    public bool Disposed { get; private set; }
    internal Action<PhotoEyeRequest>? Cleanup;
    internal TimeSpan? RequestedAt;

    public void Dispose()
    {
        if (Disposed)
            return;

        Disposed = true;
        Cleanup?.Invoke(this);
        Cleanup = null;
        OnVisualizationInit = null;
        var onDispose = OnDispose;
        OnDispose = null;
        onDispose?.Invoke();
    }
}
