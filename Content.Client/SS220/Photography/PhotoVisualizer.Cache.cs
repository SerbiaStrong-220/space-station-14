// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.GameTicking;
using Content.Shared.SS220.Photography;

namespace Content.Client.SS220.Photography;

public sealed partial class PhotoVisualizer : EntitySystem
{
    // QUEUED_MODE
    // true = single map, so use photo rendering queue
    // false = multiple maps, no queue. Only when client-side maps are implemented (currently having issues with MapId conflicts)
    private static readonly bool QUEUED_MODE = true;

    private Dictionary<string, HashSet<PhotoEyeRequest>> _eyeRequests = new();
    private Queue<PhotoEyeRequest> _eyeRequestQueue = new();
    private Dictionary<string, PhotoData> _photoDataCache = new();

    private void InitializeCache()
    {
        SubscribeNetworkEvent<PhotoDataRequestResponse>(OnPhotoDataReceived);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent args)
    {
        foreach (var (id, requests) in _eyeRequests)
        {
            foreach (var request in requests)
                request.Dispose(); // slower than could be but not too bad
        }

        _eyeRequestQueue.Clear();
    }

    private void OnPhotoDataReceived(PhotoDataRequestResponse args)
    {
        if (!_eyeRequests.TryGetValue(args.Id, out var requestsSet) || requestsSet.Count == 0)
            return;

        if (args.Data is not { } data || !data.Valid)
        {
            _sawmill.Warning($"Photo data with ID {args.Id} was invalid. Disposing requests.");
            foreach (var request in requestsSet)
            {
                request.Dispose();
            }
            return;
        }

        if (_photoDataCache.TryAdd(data.Id, data))
            Log.Debug("Photo data with ID {0} was received and cached.", data.Id);
        else
            Log.Warning("Photo data with ID {0} was received but has already been cached!", data.Id);

        if (QUEUED_MODE)
        {
            UpdateQueue();
        }
        else
        {
            if (TryGetVisualization(data, out var eye))
            {
                foreach (var request in requestsSet)
                {
                    request.OnVisualizationInit?.Invoke(eye, data.PhotoSize);
                }
            }
        }
    }

    public void UpdateQueue()
    {
        var runNext = true;

        while (runNext)
        {
            runNext = false;

            if (!_eyeRequestQueue.TryPeek(out var request))
                return;

            // just clean up and do next if request was already disposed
            if (request.Disposed)
            {
                _eyeRequestQueue.Dequeue();
                runNext = true;
                continue;
            }

            if (!_photoDataCache.TryGetValue(request.PhotoId, out var photoData))
                return; //we wait

            if (!photoData.Valid)
            {
                request.Dispose();
                return;
            }

            if (TryGetVisualization(photoData, out var eye))
            {
                request.OnVisualizationInit?.Invoke(eye, photoData.PhotoSize);
            }
        }
    }

    public void ForgetRequestAndCleanup(PhotoEyeRequest request)
    {
        if (!_eyeRequests.TryGetValue(request.PhotoId, out var requestSet))
            return;

        requestSet.Remove(request);
        DereferenceIfGargabe(request.PhotoId);
    }

    private void DereferenceIfGargabe(string id)
    {
        if (!_eyeRequests.TryGetValue(id, out var requestSet))
            return;

        if (requestSet.Count == 0)
        {
            DisposeVisualization(id);
            _photoDataCache.Remove(id);
            _eyeRequests.Remove(id);
        }
    }

    public void RequestPhotoEye(PhotoEyeRequest request)
    {
        Log.Debug("Eye requested! ID:"+request.PhotoId);
        var requestNeeded = false;
        if (!_eyeRequests.TryGetValue(request.PhotoId, out var requestSet))
        {
            requestSet = new();
            _eyeRequests.Add(request.PhotoId, requestSet);
            requestNeeded = true;
        }

        requestSet.Add(request);

        if (!QUEUED_MODE)
        {
            if (_currentlyVisualized.TryGetValue(request.PhotoId, out var photoVis))
            {
                request.OnVisualizationInit?.Invoke(photoVis.Eye, photoVis.Size);
                return;
            }
        }

        _eyeRequestQueue.Enqueue(request);
        _sawmill.Debug($"Queued a photo render request. Queue length now: {_eyeRequestQueue.Count}");

        if (_photoDataCache.TryGetValue(request.PhotoId, out var photoData))
        {
            if (!QUEUED_MODE)
            {
                if (TryGetVisualization(photoData, out var eye))
                    request.OnVisualizationInit?.Invoke(eye, photoData.PhotoSize);
            }
            else
            {
                UpdateQueue();
            }
            return;
        }

        if (requestNeeded)
        {
            var ev = new PhotoDataRequest(request.PhotoId);
            RaiseNetworkEvent(ev);
            _sawmill.Debug($"Sending data request for ID {request.PhotoId}");
        }
    }
}

public sealed class PhotoEyeRequest
{
    public readonly string PhotoId;
    public OnVisualizationInitCallback? OnVisualizationInit;
    public OnDisposeCallback? OnDispose;
    public bool Disposed { get; private set; } = false;

    public PhotoEyeRequest(string photoId)
    {
        PhotoId = photoId;
    }

    public void Dispose()
    {
        Disposed = true;
        OnDispose?.Invoke();
        var sysMan = IoCManager.Resolve<IEntitySystemManager>();
        var photoVisualizer = sysMan.GetEntitySystem<PhotoVisualizer>();
        photoVisualizer.ForgetRequestAndCleanup(this);

        //giga evil recursion when disposed from UpdateQueue, but should be OK unless player opens a fuckton of photos
        photoVisualizer.UpdateQueue();
    }

    public delegate void OnVisualizationInitCallback(EyeComponent eye, float size);
    public delegate void OnDisposeCallback();
}
